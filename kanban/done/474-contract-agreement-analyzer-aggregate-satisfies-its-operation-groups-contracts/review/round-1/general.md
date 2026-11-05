# Round 1 — general
**Date:** 2026-11-05
**Scope reviewed:** branch vs master (framework scope): `contract-agreement-analyzer.cs`,
`contract-agreement-types.cs`, the two new attributes, AnalyzerReleases, the Profile/Roles contract
renames and their SPA/test call sites, the analyzer tests and fixture, and the two skill edits.

## Summary

The change adds `ContractAgreementAnalyzer` (TWA0039–TWA0043), a CompilationAction that links an
aggregate root to `[Operation]` groups by slice namespace and checks details members, Set-list
mutations, and query projections. It also fixes the two real drifts on the contract side
(Profile `Alias` → `DisplayName`, Roles `PermissionIds` → `Permissions`). The analyzer logic matches
its Design regions and the task's four checks. The implementation agrees with both the task and the
implementer restatement. Choosing convention-only linking was one of the two options the task
offered, so it is not a misread.

Re-verified in the claim worktree:

- `./bin/dev build`: 0 warnings, 0 errors.
- Analyzer tests (`tests/analyzers/timewarp-architecture-analyzers-tests`, `dotnet run`):
  total 289, failed 0.
- `dotnet msbuild -getItem:Analyzer` on web-application lists the convention analyzers project.
  web-application references web-contracts and web-domain directly. web-domain does not reference
  web-contracts, so web-application is the compilation that reports.
- No `ProfileAlias` / `.alias` / `permissionIds` leftovers in non-C# sources (razor, ts, js, json).
- `source/Directory.Build.props` is at 2.0.0-beta.32. That version is unreleased, so the analyzer ships
  in it, as Results says.

Risk is low: no bugs found. The open items are one vacuous test and some leftover wording.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/analyzers/timewarp-architecture-analyzers-tests/contract-agreement-analyzer-tests.cs:271
- Description: `Given_Group_With_No_Root_In_Its_Slice_IsClean` is vacuous. Its comment says
  "Contracts drift freely when no root lives in the slice namespace", but the test introduces no
  drift. `RoleContracts` already agree with `RoleDomain`, so the expected clean result holds whether
  or not the namespace link works. If `SliceNamespace` / the `linked` filter were deleted, the test
  would still pass. It also uses a bare `string.Replace` (line 274, and line 60) instead of `Edit`.
  The file's Design region says every replaced text is asserted, so a fixture edit could silently
  turn the namespace move into a no-op.
- Suggestion: Use `Edit` for both replacements. Introduce real drift in the unlinked case (for
  example, rename the contracts' `Permissions` back to `PermissionIds`, which flags TWA0041 when
  linked). Then the clean result proves an unlinked group is not checked.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/features/profile/get-profile/get-profile-contracts.cs:2
- Description: The rename left "alias" in prose and identifiers. The Purpose region still says
  "display profile (alias, prefs, avatar)". `ShellUserCard.razor:56` still names the parameter
  `alias` (`ShellUserName(string? identityName, string? alias)`) although it now receives
  `ProfileState.DisplayName`. The task's point is that both sides use one name.
- Suggestion: Say "display name" in the Purpose line and rename the `ShellUserName` parameter to
  `displayName`. Mock data values ("alias") can stay.
- Status: open
