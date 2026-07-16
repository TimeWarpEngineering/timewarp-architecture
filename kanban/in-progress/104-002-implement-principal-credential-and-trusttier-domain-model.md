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

## Session

- Created: 2026-07-16
