# First human passkey create claims Administrator (atomic empty-deploy bootstrap)

## Description

Product rule: when the deployment has no stored Administrator yet, the first successful
**human passkey** Create account claims Administrator + Member. Atomic at the role store.
No kill-switch — empty DB is not protected value. Bootstrap PrincipalIds stay break-glass.

## Checklist

- [x] `IPrincipalRoleStore.TryClaimFirstAdministratorAsync`
- [x] InMemory (lock) + EF (Serializable transaction)
- [x] CompletePasskeyRegistration handler claims after credential attach
- [x] Host-free tests (first wins, second is Member)
- [ ] Commit

## Session

- Rejected kill-switch and over-weight race narrative; dual concurrent first admin is not a product risk worth config surface.
