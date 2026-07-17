# Implement Principal Credential and TrustTier domain model

## Parent

104

## Description

Core domain: Principal (Id Guid, Kind Human|Agent|Service, TrustTier, CreatedAt), Credential (type Passkey|AgentKey|…, public material, PrincipalId), optional display/profile nullables. No registration-form fields required. Put Design regions on types: hybrid server id + keys; profile later.

**Reopened for fold-in** of RFC ballot resolutions (agent-collaboration: same task through fold-in — no sibling “apply RFC” task).

## Requirements

- PrincipalId is stable Guid, never recycle
- Kind and TrustTier enums explicit
- Multiple credentials allowed by model (enforced in 005)
- Persistence strategy chosen (EF/in-memory for tests) documented in Design region
- RFC stop-the-line resolutions landed in code before this task is done again (see Checklist fold-in)

## Checklist

### Initial domain (shipped)
- [x] Types + enums
- [x] Storage abstraction or EF config as needed
- [x] Design regions capture hybrid identity + tiers
- [x] Unit tests for invariants

### RFC fold-in (same task — before re-done)
- [ ] **D1** Trust: orthogonal quarantine flag; constrained Promote/Quarantine/ClearQuarantine (no free SetTrustTier); birth floor; named predicates (`IsFundedAndActive`)
- [ ] **D2** Enum zeros: reserve `0 = None/Unknown`; reject None at domain entry
- [ ] **D3** Add `CredentialId`; store APIs use it
- [ ] **D7** Delete dead `UpdateCredentialAsync` handle-migration branch; document immutable Type/Handle
- [ ] Design regions updated for D1–D4, D6–D8 as resolved
- [ ] Unit tests updated; `dev build` 0/0; identity tests green
- [ ] RFC banner: folded in; Results cover ballot **and** fold-in

### Explicit deferrals (not fold-in blockers)
- [x] **D4** Store snapshots → keep shared refs; document LWW only
- [x] **D5** TimeProvider → defer to 104-006
- [x] **D6** Concurrency token → LWW docs only
- [x] **D8** Material type → keep `byte[]` copy-on-get

## Notes

Trust tiers: Keyed (has credential), Funded (paid/credit), later Established/Quarantined. No human required for Agent principals. **RFC D1 supersedes free SetTrustTier** — see `rfc/rfc.md`.

### Depends on

104-001

### Decision workspace

- Working RFC: [`rfc/rfc.md`](rfc/rfc.md) (ballots tallied 2026-07-17)
- Post-impl review: [`2026-07-16-code-review.md`](2026-07-16-code-review.md)
- Fold-in happens **on this task id** (agent-collaboration / rfc-ballot). Archived process residue **104-026** was incorrect.

### Implementation plan (104-002) — historical

#### Decisions

- Package stays **dependency-free** (no Foundation.Domain, no EF, no GuardClauses package).
- **`PrincipalId`** readonly record struct wrapping Guid; `New()` → `Guid.CreateVersion7()`; reject Empty.
- Enums: `PrincipalKind` (Human|Agent|Service), `TrustTier` (Keyed|Funded|Established|Quarantined), `CredentialType` (Passkey|AgentKey) — explicit ints. **(D2 will renumber with reserved 0.)**
- Separate **Principal** and **Credential** entities (1:N); multi-cred by model.
- Persistence: **`IPrincipalStore` + `InMemoryPrincipalStore`** only. EF later in host. Document in Design regions.
- Validation: BCL `Argument*Exception.ThrowIf*`.

#### Package files (flat under `source/libraries/timewarp-identity/`)

- `principal-id.cs`, `principal-kind.cs`, `trust-tier.cs`, `credential-type.cs`
- `principal.cs`, `credential.cs`
- `i-principal-store.cs`, `in-memory-principal-store.cs`
- Namespace `TimeWarp.Identity` everywhere

#### Principal

- Factory `Create(kind)` → New Id, TrustTier.Keyed, CreatedAt UTC
- Optional `DisplayName`; `SetDisplayName`, `SetTrustTier` (defined enum only) — **SetTrustTier removed by D1 fold-in**
- No email/password required; Agent needs no human

#### Credential

- Id Guid (v7) — **becomes CredentialId (D3)**; PrincipalId, Type, Handle byte[] (non-empty, defensive copy), PublicMaterial byte[] (non-empty, copy), CreatedAt, RevokedAt?, Label?
- `Create(...)`; `Revoke` once (second throws)

#### IPrincipalStore

Add/Get/Update principal; Add/Get/List/FindByHandle/Update credential. Uniqueness: principal id; (type, handle). Require principal exists for credential add. Multi-cred allowed.

#### Tests

`tests/libraries/timewarp-identity-tests/` — Fixie + Shouldly + TimeWarp.Fixie, ProjectReference Identity. Suites: PrincipalId, Principal, Credential, InMemoryPrincipalStore. Wire slnx under `!foundationPackages` gate with libraries.

#### Out of scope

WebAuthn (003), agent tokens (004), list/revoke APIs (005), ceremony tests (006), 402, EF, app wiring, ADRs/skills.

#### Verify

`dev build` 0/0; `dotnet fixie tests/libraries/timewarp-identity-tests`

## Session

- Created: 2026-07-16
- Plan: 2026-07-16 (orchestrate-task 104-002)
- Implementation: 2026-07-16
- Review: 2026-07-16 (bugs fixed; re-verified)
- Architecture review: 2026-07-16 — [2026-07-16-code-review.md](2026-07-16-code-review.md); items 1–4 to resolve before 104-003
- RFC ballot: 2026-07-17 (3 reviewers: A, B, adversarial-C)
- Reopened for fold-in: 2026-07-17 (agent-collaboration same-task rollout; archived erroneous 104-026)

## Results

### Summary (implementation pass — pre fold-in)
Implemented Principal / Credential / TrustTier domain model in dependency-free **TimeWarp.Identity**: value id, enums, entities, `IPrincipalStore` + `InMemoryPrincipalStore`, Fixie+Shouldly unit tests.

**Task was marked done too early** — RFC resolutions not yet in product truth. Final Results after fold-in will replace/extend this section.

### Files changed (implementation pass)
| Action | Path |
|--------|------|
| Created | `source/libraries/timewarp-identity/principal-id.cs` |
| Created | `source/libraries/timewarp-identity/principal-kind.cs` |
| Created | `source/libraries/timewarp-identity/trust-tier.cs` |
| Created | `source/libraries/timewarp-identity/credential-type.cs` |
| Created | `source/libraries/timewarp-identity/principal.cs` |
| Created | `source/libraries/timewarp-identity/credential.cs` |
| Created | `source/libraries/timewarp-identity/i-principal-store.cs` |
| Created | `source/libraries/timewarp-identity/in-memory-principal-store.cs` |
| Created | `tests/libraries/timewarp-identity-tests/**` |
| Edited | `timewarp-architecture.slnx` |
| Edited | `.template.config/template.json` (`tests/libraries/**` exclude under foundationPackages) |

### Key decisions (implementation pass)
- Package remains dependency-free (no Foundation.Domain, EF, GuardClauses)
- Persistence port only: `IPrincipalStore` + ConcurrentDictionary store; EF later in host
- Handle uniqueness key is `(CredentialType, hex(handle))`
- BCL `Argument*Exception` / `InvalidOperationException` for invariants
- `DateTimeOffset` for timestamps (not `DateTime`)
- Credential getters return array **copies**; empty `PrincipalId` rejected at `Credential.Create`
- In-memory store documents reference semantics (shared entity instances)

### Build / tests (implementation pass)
- `dev build`: **0 warnings, 0 errors**
- `dotnet fixie tests/libraries/timewarp-identity-tests`: **35 passed** (after review fixes)

### Review
- First pass: bugs (mutable getters, empty PrincipalId), suggestions (DateTimeOffset, store semantics)
- Fixes applied; tests expanded for getter immutability, empty id, undefined type
- Architecture review: cascade items 1–4 → RFC ballot

### RFC ballot (2026-07-17)

Working material: [`rfc/rfc.md`](rfc/rfc.md)

| # | Resolution | Fold-in |
|---|------------|---------|
| 1 Trust | C refined (quarantine flag + constrained transitions + birth floor) | **this task** |
| 2 Enum zeros | B renumber with reserved 0 | **this task** |
| 3 CredentialId | B add wrapper | **this task** |
| 4 Store snapshots | A keep shared refs; document LWW | Design only (done when Design updated) |
| 5 TimeProvider | Defer to 104-006 | deferred |
| 6 Concurrency | A LWW docs only | Design only |
| 7 Update handle | B delete dead branch | **this task** |
| 8 Material type | A byte[] copies | keep as-is |

Downstream **104-003** stays backlog until this task is done with fold-in complete.
