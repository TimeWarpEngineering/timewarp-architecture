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
- [x] **D1** Trust: orthogonal quarantine flag; constrained Promote/Quarantine/ClearQuarantine (no free SetTrustTier); birth floor; named predicates (`IsFundedAndActive`)
- [x] **D2** Enum zeros: reserve `0 = None/Unknown`; reject None at domain entry
- [x] **D3** Add `CredentialId`; store APIs use it
- [x] **D7** Delete dead `UpdateCredentialAsync` handle-migration branch; document immutable Type/Handle
- [x] Design regions updated for D1–D4, D6–D8 as resolved
- [x] Unit tests updated; `dev build` 0/0; identity tests green
- [x] RFC banner: folded in; Results cover ballot **and** fold-in

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

### Fold-in implementation plan (2026-07-17)

Source of truth: `rfc/rfc.md` §7.1. No further ballot.

#### D2 — Enum zeros (do first — renumbers cascade)
- `PrincipalKind`: None=0, Human=1, Agent=2, Service=3
- `TrustTier` (progression only — remove Quarantined): None=0, Provisional=1, Keyed=2, Funded=3, Established=4
- `CredentialType`: None=0, Passkey=1, AgentKey=2
- Reject None at Create / Promote / Credential.Create

#### D1 — Trust model (C refined)
- `Principal.IsQuarantined` bool (default false)
- Remove `SetTrustTier`
- `Promote(TrustTier target)` — only forward progression among Provisional→Keyed→Funded→Established; reject None/quarantine coupling; reject if quarantined
- `Quarantine()` / `ClearQuarantine()`
- Birth: `Create` → TrustTier.Provisional, IsQuarantined=false
- `MarkKeyedOnFirstCredential()` or store calls Promote(Keyed) when first cred added — implement on Principal: `RecordCredentialAttached()` promotes Provisional→Keyed if not quarantined
- Named predicates: `IsFundedAndActive` => !IsQuarantined && TrustTier is Funded or Established; `IsKeyedOrHigherAndActive` as needed

#### D3 — CredentialId
- `credential-id.cs` mirror PrincipalId (New/From/IsEmpty)
- Credential.Id type CredentialId; store GetCredentialAsync(CredentialId)

#### D7 — UpdateCredential
- Remove handle-migration branch; Update only replaces same Id; document Type/Handle immutable; Update used for revoke/label persistence

#### Design regions
- Document D1–D4, D6–D8, D5 deferred to 104-006
- Store: LWW, shared refs, FindCredentialByHandle returns revoked

#### Tests
- Update all for new enum values, Provisional birth, quarantine, promote rules, CredentialId, no free SetTrustTier
- Reject None kinds/tiers/types


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

### Summary (fold-in complete — 2026-07-17)
RFC §7.1 resolutions landed in **TimeWarp.Identity** on this host task (no sibling apply-RFC task). Product law now matches ballot outcomes for D1 (C refined), D2, D3, D7; Design regions document D4/D6/D8 and defer D5 to 104-006.

### Fold-in what landed

| Decision | Outcome | Implementation |
|----------|---------|----------------|
| D1 Trust | C refined | `IsQuarantined`; removed `SetTrustTier`; `Promote` / `Quarantine` / `ClearQuarantine`; birth `Provisional`; `RecordCredentialAttached` Provisional→Keyed; store calls it on `AddCredentialAsync`; `IsFundedAndActive` / `IsActive` |
| D2 Enum zeros | B | `None=0` on PrincipalKind, TrustTier, CredentialType; reject None at Create/Promote/Credential.Create |
| D3 CredentialId | B | `credential-id.cs`; `Credential.Id` + `GetCredentialAsync(CredentialId)` |
| D4 Snapshots | A (Design) | Shared refs + LWW documented on store/port |
| D5 TimeProvider | Defer | Design note → 104-006 |
| D6 Concurrency | A (Design) | LWW documented; no version token |
| D7 Update handle | B | Dead reindex branch deleted; immutable Type/Handle throws if changed on Update |
| D8 Material | A | Keep `byte[]` copy-on-get |

### Promote rule (decided at fold-in)
Allow **any strictly higher** progression tier (Provisional→…→Established) when not quarantined; reject None, same, lower, and quarantined.

### Files changed (fold-in)
| Action | Path |
|--------|------|
| Edited | `source/libraries/timewarp-identity/principal-kind.cs` |
| Edited | `source/libraries/timewarp-identity/trust-tier.cs` |
| Edited | `source/libraries/timewarp-identity/credential-type.cs` |
| Edited | `source/libraries/timewarp-identity/principal.cs` |
| Edited | `source/libraries/timewarp-identity/credential.cs` |
| Edited | `source/libraries/timewarp-identity/i-principal-store.cs` |
| Edited | `source/libraries/timewarp-identity/in-memory-principal-store.cs` |
| Created | `source/libraries/timewarp-identity/credential-id.cs` |
| Edited | `tests/libraries/timewarp-identity-tests/principal-tests.cs` |
| Edited | `tests/libraries/timewarp-identity-tests/credential-tests.cs` |
| Edited | `tests/libraries/timewarp-identity-tests/in-memory-principal-store-tests.cs` |
| Created | `tests/libraries/timewarp-identity-tests/credential-id-tests.cs` |
| Edited | `rfc/rfc.md` (banner + checklist folded in) |

### Build / tests (fold-in)
- `dev build`: **0 warnings, 0 errors**
- `dotnet fixie tests/libraries/timewarp-identity-tests`: **54 passed**

### Historical — implementation pass (pre fold-in)
Domain package + in-memory store + Fixie tests shipped 2026-07-16; marked done too early before RFC. Review → RFC ballot 2026-07-17 → fold-in above.

### RFC ballot (2026-07-17)

Working material: [`rfc/rfc.md`](rfc/rfc.md) — **status: Folded in**.

| # | Resolution | Status |
|---|------------|--------|
| 1 Trust | C refined | **landed** |
| 2 Enum zeros | B | **landed** |
| 3 CredentialId | B | **landed** |
| 4 Store snapshots | A Design | **documented** |
| 5 TimeProvider | Defer 104-006 | **deferred** |
| 6 Concurrency | A LWW Design | **documented** |
| 7 Update handle | B | **landed** |
| 8 Material type | A | **kept** |

Downstream **104-003** may open once this task is marked done.
