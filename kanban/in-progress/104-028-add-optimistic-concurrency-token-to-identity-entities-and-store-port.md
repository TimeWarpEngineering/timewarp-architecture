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

- [ ] Reference foundation-domain (dual-mode); Principal + Credential adopt `Entity<TId>`
      (inherit `Version`; drop hand-rolled equality where the base now provides it)
- [ ] `ConcurrencyConflictException` in the library
- [ ] Port docs: Update* conflict contract replaces the LWW note (D6 superseded)
- [ ] In-memory store: snapshot-on-get + version check on update
- [ ] Tests: conflict throw on stale update; revoke-resurrection race test (fails under LWW,
      passes with token); quarantine and tier-demotion race coverage
- [ ] Reconcile Design regions in principal.cs, credential.cs, i-principal-store.cs,
      in-memory-principal-store.cs (remove "LWW (D6)" notes)
- [ ] Update 104-003 to list this as a dependency

## Notes

- Task 106 defines `Entity<TId>` (typed id, equality, `Version`) in foundation-domain; this task
  adopts it in identity and adds the port conflict semantics on top. The library remains
  independent of the *rest* of foundation (application/server layers) — the dependency is
  foundation-domain primitives only, and it must stay that lean.
- EF `ValueConverter`/mapping for `Version` in host stores arrives with the EF wave; nothing in
  this task references EF.

## Session

- Created: 2026-07-19
