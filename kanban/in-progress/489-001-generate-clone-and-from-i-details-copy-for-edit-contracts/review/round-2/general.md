# Round 2 — general
**Date:** 2026-11-23
**Scope reviewed:** fix delta `ba2d6cebc..2f96e5bec` (commit 2f96e5bec), with the round-1 findings
M1–M3 and the updated `## Results` / `### How to validate` record on `task.md`.

## Summary

The fix pass closes all three round-1 findings. The living-contract checks are on runfiles that
`web-jaribu-tests` already compiles: its `features/**/*-tests.cs` glob covers them. The stray
runfile is gone. `DetailsCopyClassification` now finds an explicit `object ICloneable.Clone()`
through `FindImplementationForInterfaceMember` and `ExplicitInterfaceImplementations`. That
covers TWA0047 and keeps the generator from emitting a second Clone. It is the single linked copy
of the predicates, used by the details-copy analyzer, `ContractAgreementAnalyzer`, and the
generator. A grep of `source/` for the old predicate shapes finds them only in the shared file.
One new nit: the GetRole check landed in `create-role-tests.cs`, and that runfile's
regions do not mention it.

## Prior findings re-verified

- **M1 — fixed.** `get-profile-tests.cs` (`GetProfileResponse_Given_`), `update-site-settings-tests.cs`
  (`UpdateSiteSettingsCommand_Given_`), and `create-role-tests.cs` (new `GetRoleResponse_Given_`)
  carry the acceptance checks. `tests/container-apps/web/web-jaribu-tests/web-jaribu-tests.csproj`
  globs `features/**/*-tests.cs`. `tests/analyzers/details-copy-contracts-tests.cs` is deleted.
  `### How to validate` lists the three runfiles and records their passes (15, 6, 12 tests). The
  stated reason for not adding `get-role-tests.cs` holds: `.timewarp/suppression-baseline.json`
  counts suppressions per file, so a new runfile's `CA1707` `NoWarn` would be above the baseline.
- **M2 — fixed.** `CloneMethods` returns the explicit implementation, and `CloneReturnsThis` and
  `IdentityCloneLocations` both use it. `HasClone` on the generator side uses the same method, so
  `EmitClone` stays false and no `public object Clone()` is emitted. The new analyzer test
  `Explicit_Identity_Clone_Flags_TWA0047` and the generator test
  `Explicit_Identity_Clone_Is_Not_Replaced` cover both sides. The generator test's
  `ShouldNotContain("object Clone()")` matches the emitter's `public object Clone()` line
  (`details-copy-emitter.cs:215`), so it would fail if the Clone came back. Both tests are recorded
  as passing in `## Results`.
- **M3 — fixed.** `source/analyzers/shared/details-copy-classification.cs` is linked into both
  `timewarp-architecture-convention-analyzers.csproj` and `foundation-contracts-generators.csproj`.
  The private copies in `details-copy-analyzer.cs`, `details-copy-symbols.cs`, and
  `contract-agreement-analyzer.cs` are removed.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs:8-17, :136
- Description: `GetRoleResponse_Given_` (the GetRole Clone acceptance check) is in the CreateRole
  runfile. The leading comment and `#region Purpose` still say the file covers CreateRole only
  ("for CreateRole"). Nothing in the file says that a GetRole check lives here, or why it is not
  in a `get-role` runfile (the CA1707 per-file suppression baseline). Only `task.md` records it.
  An agent looking for GetRole coverage beside `get-role-contracts.cs` will not find it. An agent
  tidying placement could move it into a new `get-role-tests.cs` and add the suppression that the
  fix pass avoided. The context-regions maintenance rule (TWA0004, `tw-agent-context-regions`)
  says to keep Purpose accurate when a file's contents change. The fix pass already did this for
  `update-site-settings-tests.cs`.
- Suggestion: Update the Purpose region (and the leading comment) of `create-role-tests.cs` so
  it names the GetRole Clone check. Add a short Design note: the check sits here because a
  separate `get-role-tests.cs` would add a `CA1707` suppression above the per-file baseline.
- Status: open

## Commands run

None beyond read-only `git show`, `grep`, and file reads. No build, test, audit, or hooks were run.
