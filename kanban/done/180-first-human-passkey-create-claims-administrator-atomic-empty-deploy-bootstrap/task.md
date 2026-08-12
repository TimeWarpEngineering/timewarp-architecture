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
- [x] Commit (`5a225078`; sign-in claim removed `eee50e03` / `1819a600`)
- [x] Implementation review (`review/` — accepted-exceptions)

## Results

Create-only first-admin is live:

- `TryClaimFirstAdministratorAsync` — in-memory lock; EF Serializable EXISTS + write.
- Called only from `CompletePasskeyRegistration` **after** successful `AddCredentialAsync`.
- Sign-in and agent registration do **not** claim (greenfield wipe to re-bootstrap).
- Host-free: first wins, second is effective Member.
- EF: `TryClaimFirstAdministrator_first_wins_second_stays_unassigned` added on review.

**Review:** effort 1, round 1. Disposition **accepted-exceptions** (`review/disposition.md`).
M1 fixed (EF first-wins test). M2 wontfix (sign-in/fail paths structurally cannot claim).

### How to validate

**Automated**

```bash
dotnet run source/container-apps/web/features/admin/principals/effective-roles-resolver-tests.cs
# expect: InMemoryPrincipalRoleStore_FirstAdministrator_* pass

cd tests/container-apps/web/web-infrastructure-tests && dotnet test -c Release \
  -- --filter-method TryClaimFirstAdministrator
# expect: pass when Postgres/Docker available (CI fail-closed)
```

**Expect**

- First Create on empty store → stored Administrator + Member.
- Second Create → no stored roles; effective Member.
- Sign-in of a Member principal does not grant Administrator.

**Not in scope:** claim-on-sign-in backfill; kill-switch config.

## Notes

- Review: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`
- Rejected kill-switch; dual concurrent first admin is not a product risk worth config.

## Session

- Implement: `5a225078` feat; revert sign-in claim `eee50e03` / `1819a600`; docs `d302ac46`.
- 2026-08-12: Grok implementation review (folderized; round 1 general; disposition accepted-exceptions).
