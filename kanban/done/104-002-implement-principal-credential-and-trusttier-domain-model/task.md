# Implement Principal Credential and TrustTier domain model

## Parent

104

## Description

Core domain: Principal (Id Guid, Kind Human|Agent|Service, TrustTier, CreatedAt), Credential (type Passkey|AgentKey|…, public material, PrincipalId), optional display/profile nullables. No registration-form fields required. Put Design regions on types: hybrid server id + keys; profile later.

## Requirements

- PrincipalId is stable Guid, never recycle
- Kind and TrustTier enums explicit
- Multiple credentials allowed by model (enforced in 005)
- Persistence strategy chosen (EF/in-memory for tests) documented in Design region

## Checklist

- [x] Types + enums
- [x] Storage abstraction or EF config as needed
- [x] Design regions capture hybrid identity + tiers
- [x] Unit tests for invariants

## Notes

Trust tiers: Keyed (has credential), Funded (paid/credit), later Established/Quarantined. No human required for Agent principals.

### Depends on

104-001

### Implementation plan (104-002)

#### Decisions

- Package stays **dependency-free** (no Foundation.Domain, no EF, no GuardClauses package).
- **`PrincipalId`** readonly record struct wrapping Guid; `New()` → `Guid.CreateVersion7()`; reject Empty.
- Enums: `PrincipalKind` (Human|Agent|Service), `TrustTier` (Keyed|Funded|Established|Quarantined), `CredentialType` (Passkey|AgentKey) — explicit ints.
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
- Optional `DisplayName`; `SetDisplayName`, `SetTrustTier` (defined enum only)
- No email/password required; Agent needs no human

#### Credential

- Id Guid (v7), PrincipalId, Type, Handle byte[] (non-empty, defensive copy), PublicMaterial byte[] (non-empty, copy), CreatedAt, RevokedAt?, Label?
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

## Results

### Summary
Implemented Principal / Credential / TrustTier domain model in dependency-free **TimeWarp.Identity**: value id, enums, entities, `IPrincipalStore` + `InMemoryPrincipalStore`, Fixie+Shouldly unit tests.

### Files changed
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

### Key decisions
- Package remains dependency-free (no Foundation.Domain, EF, GuardClauses)
- Persistence port only: `IPrincipalStore` + ConcurrentDictionary store; EF later in host
- Handle uniqueness key is `(CredentialType, hex(handle))`
- BCL `Argument*Exception` / `InvalidOperationException` for invariants
- `DateTimeOffset` for timestamps (not `DateTime`)
- Credential getters return array **copies**; empty `PrincipalId` rejected at `Credential.Create`
- In-memory store documents reference semantics (shared entity instances)

### Build / tests
- `dev build`: **0 warnings, 0 errors**
- `dotnet fixie tests/libraries/timewarp-identity-tests`: **35 passed** (after review fixes)

### Review
- First pass: bugs (mutable getters, empty PrincipalId), suggestions (DateTimeOffset, store semantics)
- Fixes applied; tests expanded for getter immutability, empty id, undefined type

### Next
104-003 WebAuthn passkey register/authenticate
