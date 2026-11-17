# Round 2 — general
**Date:** 2026-11-05
**Scope reviewed:** fix delta `7567cd78f..391d56464` (round-1 base.sha to HEAD), product paths only:
`contract-agreement-analyzer-tests.cs`, `get-profile-contracts.cs`, `ShellUserCard.razor`.

## Summary

The fix pass addresses both round-1 findings and adds nothing else to product code.
`Given_Group_With_No_Root_In_Its_Slice_IsClean` now applies the same drift that the linked-root
tests flag (`Permissions` → `PermissionIds` on GetRole and GetRoles, `[NotAggregate]` removed from
DeleteRole). All its edits go through the asserting `Edit` helper, so the test is no longer vacuous.
The other bare `string.Replace` (line 60) uses `Edit` too, and the Design region describes `Edit`.
The only `.Replace(` left in the file is inside `Edit` itself (line 32). The Purpose line says
"display name", and `ShellUserName` takes `displayName`. Its one caller (line 29) is positional, and
no `alias` identifier is left in the component. Low risk.

## Verification

- Prior M1: fixed. The `Permissions` edit matches both list properties in the fixture
  (`contract-agreement-test-fixture.cs:191`, `:206`), and the `[NotAggregate]` text matches `:169`.
  `Edit` asserts each one is present, so a fixture edit cannot silently make the test vacuous again.
  The same `Permissions` edit with the root in the slice is the flagged case at
  `contract-agreement-analyzer-tests.cs:136`.
- Prior M2: fixed, as described above.
- `dotnet run --project tests/analyzers/timewarp-architecture-analyzers-tests -- --filter-class Contract_Agreement`:
  total 20, succeeded 20, failed 0.

## Issues

None.
