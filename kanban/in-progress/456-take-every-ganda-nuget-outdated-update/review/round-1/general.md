# Round 1 — general
**Date:** 2026-10-30
**Scope reviewed:** same as framework

## Summary

The change bumps 21 of the 22 reported pins in `Directory.Packages.props`. It moves `dotnet-ef` with
the EF family, and it bumps `<Version>` and the eleven platform pins to 2.0.0-beta.30. Microsoft.OpenApi
stays on 2.13.0 with an NU1608 rationale, and task 257 has a dated note. No `VersionOverride` was added,
nothing was downgraded, and no other pin file holds these ids. Re-verified in this worktree:
`./bin/dev build` reports 0 warnings and 0 errors, and `./bin/dev check-version` passes (source is one
prerelease ahead of 2.0.0-beta.29). This review did not re-run `dev test` or `dev template-smoke`, so
their results are the implementer's claim. One finding remains: the pin comment block was appended to
but not corrected, so it still states the old versions as current.

The implementer's `## Goal (implementer restatement)` matches the task. No misread.

## Issues

### Issue 1 — Severity: bug
- File: Directory.Packages.props:166
- Description: The task requires "Update any pin comments so they match the new versions." The comment
  block above the `Microsoft.OpenApi` pin still says, in the present tense, that EF Core, the dotnet-ef
  tool, and Npgsql.EntityFrameworkCore.PostgreSQL "stay on the RC2 train from task 389". It says
  "nuget.org has no Npgsql RC3 (latest 11.0.0-rc.2)", that Npgsql "pins Microsoft.EntityFrameworkCore
  exactly at 11.0.0-rc.2.26504.105", and that "System.ServiceModel.Primitives ... stays 10.0.652802"
  (lines 166–173). All of these are now false: the pins are RC3 and 10.1.0. The new task-456 paragraph
  is appended after them and contradicts them. A reader who stops at the first paragraph sees the
  wrong state, and a future agent could act on it. The new paragraph also repeats, almost word for
  word, the task-420 OpenApi re-check right above it.
- Suggestion: Rewrite the present-tense EF/Npgsql paragraph and the System.ServiceModel.Primitives
  sentence so they describe the RC3 / 10.1.0 state. For example: EF family, DataProtection.EFCore,
  HealthChecks.EFCore, dotnet-ef, and Npgsql are on RC3 as of task 456, and Npgsql 11.0.0-rc.3 pins
  EF Core exactly at 11.0.0-rc.3.26522.148, so they move together. ServiceModel.Primitives is on
  10.1.0 and has no 11.x. Shorten the task-456 OpenApi re-check to what is new: the NU1608 restore
  result and the date. Keep the historical "Re-checked task N" lines as they are.
- Status: open
