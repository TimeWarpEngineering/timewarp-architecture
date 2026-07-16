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

- [ ] Types + enums
- [ ] Storage abstraction or EF config as needed
- [ ] Design regions capture hybrid identity + tiers
- [ ] Unit tests for invariants

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
