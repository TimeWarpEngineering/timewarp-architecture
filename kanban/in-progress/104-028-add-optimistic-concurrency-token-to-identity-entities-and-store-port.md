# Add optimistic concurrency token to identity entities and store port

## Description

Revisit 104-002 RFC Decision 6 (unanimous A: defer concurrency token, document last-write-wins).
The deferral's own trigger condition — "version when multi-request races land" — fires inside
this program: 104-003/104-004 are the multi-request handlers and EF is committed. This is known
tech debt, not YAGNI, and the LWW races are not consistency polish — they let the store silently
violate invariants the domain enforces:

- **Revoke resurrection** (security): domain enforces one-shot `Revoke`, but full-entity LWW
  `UpdateCredentialAsync` lets a stale writer (e.g. concurrent label edit holding an older
  snapshot) save `RevokedAt = null` back — a revoked authentication credential comes back to life.
- **Quarantine loss** (security): risk path sets `IsQuarantined = true`; a stale concurrent write
  clears it.
- **Tier demotion**: `Promote` enforces strictly-upward progression per instance; a stale writer
  saves the old lower tier back.

Wave 1's in-memory store masks all three: it keeps shared references (D4), so every caller
mutates the same instance and LWW never manifests — no current test can catch these. EF breaks
exactly that (per-request tracked instances make stale overwrite real).

Scope is B-lite, not full concurrency design: token + port conflict semantics. Per-callsite
conflict *policy* (retry vs reload vs fail the request) stays deferred to the handlers that hit
it — that part of the D6 lean was correct.

## Requirements

- **Depends on 106** (foundation entity-primitive modernization). Decision 2026-07-19:
  timewarp-identity's foundation-independence is dropped — foundation-domain is itself a published
  `TimeWarp.Foundation.*` package, so referencing it is a normal package dependency (ASP.NET
  Identity -> Microsoft.Extensions.* precedent). Sequencing: 106 -> this task -> 104-003.
- timewarp-identity references foundation-domain (dual-mode: ProjectReference in-repo,
  PackageReference in package mode — accepts the release-ordering cost 104-027 already pays for
  Generators). `Principal` and `Credential` adopt `Entity<TId>` and inherit `Version` from it —
  store-owned, incremented on successful update; not settable by domain code. Do NOT define a
  separate identity-local Version.
- `UpdatePrincipalAsync` / `UpdateCredentialAsync` defined to throw a library-owned
  `ConcurrencyConflictException` on version mismatch. Port semantics documented in the
  `IPrincipalStore` Design region (replacing the LWW note).
- In-memory store enforces the check. Knock-on: this forces revisiting D4 shared references —
  version checks are meaningless when every caller mutates the same instance, so the in-memory
  store moves to snapshot-on-get semantics (accepted scope).
- Rejected alternative, for the record: host-side EF shadow `rowversion` needs no library change,
  but then the port has no conflict semantic — `DbUpdateConcurrencyException` (an EF type) leaks
  into handlers, and in-memory vs EF stores diverge in behavior. Concurrency semantics belong on
  the port both stores implement.
- Timing: do this before 104-003 handlers are written and before the package ships — one store
  implementation, zero handlers, zero external consumers today. After publish, changing
  `IPrincipalStore` update semantics is a breaking change for every implementor (same cost
  asymmetry that justified 104-027).

## Checklist

- [x] Reference foundation-domain (dual-mode); Principal + Credential adopt `Entity<TId>`
      (inherit `Version`; drop hand-rolled equality where the base now provides it)
- [x] `ConcurrencyConflictException` in the library
- [x] Port docs: Update* conflict contract replaces the LWW note (D6 superseded)
- [x] In-memory store: snapshot-on-get + version check on update
- [x] Tests: conflict throw on stale update; revoke-resurrection race test (fails under LWW,
      passes with token); quarantine and tier-demotion race coverage
- [x] Reconcile Design regions in principal.cs, credential.cs, i-principal-store.cs,
      in-memory-principal-store.cs (remove "LWW (D6)" notes)
- [x] Update 104-003 to list this as a dependency (already present — verified)

## Notes

### Implementation plan (2026-07-19)

#### Investigation summary (facts the plan relies on)

- `Entity<TId>`: `protected Entity(TId id)`, get-only `Id`, `public long Version { get; private set; }`, exact-type+Id equality. Only writer today is the EF hook (`PropertyEntry.CurrentValue` write) — a ctor overload cannot affect it.
- `EntityVersion.Next(long)` is the pure increment seam; reuse it in the in-memory store.
- TWA0011/0012 match ONLY `IAggregateRoot` implementors. Inheriting `Entity<TId>` alone is analyzer-silent. **Decision: do NOT implement `IAggregateRoot` in this task** — identity uses guard clauses, not nested Invariants; record explicit deferral in Design regions.
- Dual-mode reference pattern exists in `web-domain.csproj`; CPM pin `TimeWarp.Foundation.Domain` 2.0.0-beta.2 already exists. Repo + CI always run ProjectReference mode; published beta.2 predating `Entity<TId>` is the same accepted release-ordering cost as 104-027/Generators.
- Identity has no `global-usings.cs`; `Entity<TId>` lives in `TimeWarp.Foundation.Entities`.
- `Credential.Label` is get-only; the "stale label editor" is modeled as any stale-snapshot writer.
- Tests: `tests/libraries/timewarp-identity-tests`, 71 tests, no `InternalsVisibleTo` — rehydration seam stays `internal`, exercised through the public store API.
- `ConcurrentDictionary.TryUpdate` CANNOT serve as version CAS: with base equality (type+Id), any same-Id snapshot compares equal — a real write lock is required.
- 104-003 already lists 104-028 under "Depends on" — checklist item just needs ticking.
- csproj `Description` makes no independence claim; the line to reconcile is "ConcurrentDictionary keeps the library dependency-free" in `in-memory-principal-store.cs`.

#### Seam design: Version write + rehydration

**foundation-domain:** add one protected ctor overload to `Entity<TId>`:

```csharp
protected Entity(TId id) : this(id, 0) { }

protected Entity(TId id, long version)
{
  ArgumentOutOfRangeException.ThrowIfNegative(version);
  Id = id;
  Version = version;
}
```

- Rehydration is construction-time only; no bump method, no mutator. Stores "increment" by constructing a NEW snapshot with `EntityVersion.Next(storedVersion)`.
- EF mechanism untouched (private setter, hook, access-mode pin all unaffected); `Profile` keeps calling `base(id)`.
- entity.cs Design region: document the rehydration ctor as the seam for non-EF stores.

**identity (same-assembly internal rehydration):**
- `principal.cs`: full-state private ctor `(PrincipalId id, PrincipalKind kind, TrustTier trustTier, bool isQuarantined, DateTimeOffset createdAt, string? displayName, long version)` chaining `base(id, version)`; `Create` passes version 0. Add `internal Principal Snapshot(long version) => new(Id, Kind, TrustTier, IsQuarantined, CreatedAt, DisplayName, version);`
- `credential.cs`: full-state private ctor incl. `revokedAt` + `version`; `Create` passes `revokedAt: null, version: 0`. Add `internal Credential Snapshot(long version) => new(Id, PrincipalId, Type, HandleField.ToArray(), PublicMaterialField.ToArray(), CreatedAt, RevokedAt, Label, version);` — ToArray copies preserve D8; private ctor does not copy (Create and Snapshot each own that).
- `Snapshot` never runs Create's invariants-on-new semantics (no id minting, no re-validation) — copies already-valid state; safe because it is `internal` and every public instance passed Create's guards.

#### Port contract (for i-principal-store.cs Design region — replaces D6 LWW and D4 shared-refs lines)

> **Concurrency (supersedes D6 last-write-wins).** Principal and Credential inherit `Entity<TId>.Version`, a store-owned optimistic-concurrency token. Version == 0 means created-but-never-updated; stores advance it by exactly 1 per successful update (`EntityVersion.Next`).
> - **Add\***: persists a snapshot as-is, including Version (0 for every publicly creatable instance).
> - **Get\*/Find\*/List\*** (supersedes D4 → A): return snapshots — fresh caller-owned instances; every call returns a new instance; mutating a returned instance changes nothing until Update*.
> - **Update\***: compares incoming Version against stored. Mismatch → `ConcurrencyConflictException` (entity type, id, expected = incoming, actual = stored); stored state untouched. Match → persists a snapshot with `Version = EntityVersion.Next(stored)`. The caller's in-hand instance is NOT modified — after a successful update it is one version stale; successive updates require re-Get. Unknown id remains `InvalidOperationException` — absence and staleness are distinct failure classes.
> - **AddCredentialAsync side effect**: first-credential rule mutates the STORED principal (Provisional → Keyed) and advances the stored principal's Version when — and only when — the tier actually changes; caller's principal untouched. A concurrent principal writer holding the pre-attach snapshot conflicts instead of silently demoting the tier.
> - **Conflict policy** (retry vs reload vs fail) stays with callers per the surviving half of D6.

#### Ordered work items

### 1. foundation-domain: rehydration ctor
- `entity.cs` — add `protected Entity(TId id, long version)` (negative guard), chain existing ctor, extend Design region.
- `entity-tests.cs` — rehydration ctor sets Version; negative version throws.

### 2. timewarp-identity: reference + usings
- `timewarp-identity.csproj` — dual-mode ItemGroup mirroring web-domain (ProjectReference foundation-domain when `UseFoundationPackages != true`, else PackageReference TimeWarp.Foundation.Domain). No CPM change.
- New `global-usings.cs` — Purpose region + `global using TimeWarp.Foundation.Entities;`

### 3. Entities adopt Entity<TId>
- `principal.cs` — `public sealed class Principal : Entity<PrincipalId>`; delete local Id; ctor/Create/Snapshot per seam design. Design region: inherited Version; base-provided equality; explicit non-adoption of IAggregateRoot (later alignment task).
- `credential.cs` — `public sealed class Credential : Entity<CredentialId>`; same; document Snapshot byte copies (D8 preserved).

### 4. ConcurrencyConflictException
- New `concurrency-conflict-exception.cs` (namespace TimeWarp.Identity, sealed, CA1032 ctors, message builder):
  `ConcurrencyConflictException(Type entityType, string entityId, long expectedVersion, long actualVersion)`; props `EntityType`, `EntityId`, `ExpectedVersion` (caller's), `ActualVersion` (store's).

### 5. In-memory store: snapshot-on-get + version check
- `private readonly Lock WriteLock = new();` — all mutation paths run check+swap under it; reads lock-free. Design region records WHY TryUpdate is unusable as CAS.
- `AddPrincipalAsync`: store `principal.Snapshot(principal.Version)`.
- `GetPrincipalAsync`: return `stored?.Snapshot(stored.Version)`.
- `UpdatePrincipalAsync`: existence → version compare (mismatch throws) → `Principals[id] = principal.Snapshot(EntityVersion.Next(stored.Version))`.
- `AddCredentialAsync`: store snapshot; first-credential rule:
  ```csharp
  Principal candidate = storedPrincipal.Snapshot(EntityVersion.Next(storedPrincipal.Version));
  candidate.RecordCredentialAttached();
  if (candidate.TrustTier != storedPrincipal.TrustTier) Principals[id] = candidate;
  ```
  (no version bump on the already-Keyed no-op — avoids spurious conflicts).
- `GetCredentialAsync`/`FindCredentialByHandleAsync`: snapshots; `ListCredentialsAsync`: `.Select(c => c.Snapshot(c.Version))`.
- `UpdateCredentialAsync`: existence → version check → type/handle immutability check → swap with Next. Document check order (staleness dominates).
- Design region rewrite: D4 → snapshot-on-get, D6 → version check; "dependency-free" line → "no EF/third-party dependencies — foundation-domain domain primitives only"; Lock rationale; first-credential bump semantics.
- `i-principal-store.cs` — full port contract text (D5/D7 lines retained).

### 6. Tests (tests/libraries/timewarp-identity-tests)

Existing changes (only one breaks):
- `Multi_credential_per_principal_is_allowed`: asserts tier on caller's instance — change to re-Get. All other 70 survive (verified per-test).
- Optionally extend `Update_persists_display_name_and_tier`: `loaded.Version == 1`, `principal.Version == 0`.

New `in-memory-principal-store-concurrency-tests.cs` (deterministic interleavings, no threads):
- Snapshot semantics: Get twice → distinct instances, Equals-equal; mutation of snapshot invisible until Update; credential byte arrays independent; Version 0 after Create, 1 after update.
- Stale principal update conflict: A/B Get v0; A updates (v1); B updates → ConcurrencyConflictException (Expected 0, Actual 1, typeof(Principal)); B re-Gets, retries → succeeds.
- Revoke-resurrection race (headline): A revokes+updates; stale B update → throws AND re-Get still IsRevoked. Fails under old shared-ref/LWW store.
- Quarantine-loss race: stale B → throws; stored still quarantined.
- Tier-demotion race: A Promote(Funded)+update; stale B → throws; stored still Funded.
- Attach-bumps-principal-version: pre-attach snapshot update → throws; second credential add does NOT bump again.
- Caller instance not advanced: after success, in-hand Version unchanged; same-instance second update throws.

`principal-tests.cs`/`credential-tests.cs`: `Version_defaults_to_zero`; different-id inequality sanity.

### 7. Closeout
- Tick checklist incl. "Update 104-003" (already listed; verify + tick).
- Publish-checklist note in task Notes: package-mode consumers need published TimeWarp.Foundation.Domain containing Entity<TId>; ship + bump CPM pin before/with first TimeWarp.Identity release. In-repo dual-mode/CI unaffected.
- `dev build` 0/0; identity + foundation-domain test projects; run web/foundation suites to confirm ctor addition breaks nothing.

#### Sequencing and risk notes

- Order: 1 → 2–3 (compile together) → 4–5 → 6.
- Trap 1: TryUpdate is NOT an atomic version CAS under identity-based equality — Lock required.
- Trap 2: bumping stored principal Version on every AddCredentialAsync (incl. no-op) creates spurious conflicts — tier-changed guard avoids it.
- Trap 3: Snapshot must include RevokedAt/IsQuarantined/DisplayName/CreatedAt — dropping any silently reintroduces the state loss the token prevents (snapshot-semantics tests cover each field).

#### Open questions

None.

- Task 106 defines `Entity<TId>` (typed id, equality, `Version`) in foundation-domain; this task
  adopts it in identity and adds the port conflict semantics on top. The library remains
  independent of the *rest* of foundation (application/server layers) — the dependency is
  foundation-domain primitives only, and it must stay that lean.
- EF `ValueConverter`/mapping for `Version` in host stores arrives with the EF wave; nothing in
  this task references EF.

### Implementation results (2026-07-19)

Work items 1–7 executed as planned; no deviations. `dev build` 0/0; `timewarp-identity-tests`
88/88 (71 original + 4 new Principal/Credential Version/inequality cases + 13 new
concurrency-scenario cases, with `Multi_credential_per_principal_is_allowed` changed to re-Get
per the plan); `foundation-domain-tests` 37/37 (+3 rehydration-ctor cases);
`foundation-application-tests` 13/13, `web-domain-tests` 26/26, `timewarp-architecture-analyzers-tests`
75/75, `web-server-integration-tests` 22 passed/1 skipped — all unaffected by the `Entity<TId>`
ctor addition, confirming no cross-project regression.

**Publish-checklist note:** package-mode consumers (`UseFoundationPackages=true` /
`UseAnalyzerPackages=true`) need a published `TimeWarp.Foundation.Domain` package that actually
contains `Entity<TId>`/`EntityVersion` (task 106) — the current CPM pin (`2.0.0-beta.2`) predates
both. Ship a `TimeWarp.Foundation.Domain` release containing them and bump the CPM pin
before-or-with the first `TimeWarp.Identity` package release that carries this task's changes;
until then, `TimeWarp.Identity` only builds in this monorepo's default ProjectReference
(dual-mode) path, same as CI. This is the same release-ordering cost already accepted for
104-027/Generators — no new risk class, just a second instance of it to track at ship time.

## Session

- Created: 2026-07-19
