# Round 2 — general
**Date:** 2026-11-05
**Scope reviewed:** the delta since round 1 (`review/round-1/base.sha` 5cc7baaca → HEAD ca572cceb).
Product change: `tests/container-apps/web/web-server-integration-tests/features/admin/roles/roles-endpoint-tests.cs`
(+19 lines). The other changes are kitchen files (task.md, round-1 artifacts).

## Summary

The fix pass adds `UpdateThenDelete_Roundtrip.Conflict_Given_Product_Role`, which closes round-1 M1.
For each `RoleIds.All` id, the test sends `DeleteRole.Command`, asserts `IsT1` with `Status == 409`,
and then asserts that `GetRole` still resolves the role. This is the change M1 asked for. The test is
in a class that is already registered (`RegisterTests<UpdateThenDelete_Roundtrip>()` through a module
initializer), so Jaribu discovers it. A refused delete does not mutate the seeded roles, so the test
has no ordering coupling with the other tests in the class that share the host. The handler
(`delete-role-handler-application.cs:37`) returns `ProductRoleNotDeletable` with status 409 before
`RemoveAsync`, which matches what the test asserts.

Re-verified: I ran `dotnet run` in `tests/container-apps/web/web-server-integration-tests`. Total 437:
436 succeeded, 0 failed, 1 skipped. This matches the implementer's claim. I did not repeat the
implementer's mutation check (handler guard disabled → 500), because the product tree is read-only for
the reviewer. The test asserts the exact status 409, so a 500 from the store's
`InvalidOperationException` would fail it.

Goal restatement vs the task: the fix delta does not touch the restatement, and round 1 found no
misread. No new misread to record.

## Issues

None. M1 (round 1) is verified fixed.
