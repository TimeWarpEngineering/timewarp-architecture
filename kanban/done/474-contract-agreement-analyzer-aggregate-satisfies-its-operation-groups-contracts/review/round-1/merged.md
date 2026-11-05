# Round 1 — merged findings
**Date:** 2026-11-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/analyzers/timewarp-architecture-analyzers-tests/contract-agreement-analyzer-tests.cs:271
- Description: `Given_Group_With_No_Root_In_Its_Slice_IsClean` introduces no drift, so it passes
  whether or not the namespace link works. It also uses a bare `string.Replace` (lines 274 and 60)
  instead of the asserting `Edit` that the file's Design region promises.
- Suggestion: Use `Edit`. Add real drift (for example `Permissions` → `PermissionIds` in the
  contracts) with the root moved out of the slice, and expect clean.
- Source: general
- Disposition notes: fixed in the fix pass. The test now drifts the contracts
  (`Permissions` → `PermissionIds` on GetRole and GetRoles, `[NotAggregate]` removed from DeleteRole)
  and moves the root out of the slice, through `Edit`. Line 60 uses `Edit` too, and the Design region
  now says `Edit`. Mutation check: with the root left in the slice, the test fails with 2 × TWA0041 and
  1 × TWA0042, so it is not vacuous.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/profile/get-profile/get-profile-contracts.cs:2
- Description: The Purpose region still says "(alias, prefs, avatar)". `ShellUserCard.razor:56`
  still names the parameter `alias` although it receives `ProfileState.DisplayName`.
- Suggestion: Write "display name" in the Purpose line and rename the parameter to `displayName`.
- Source: general
- Disposition notes: fixed. The Purpose line says "display name", and the `ShellUserName`
  parameter is `displayName`. The mock value `"alias"` is sample data, not a name, and stays.

## Duplicates / conflicts

- None (single reviewer).
