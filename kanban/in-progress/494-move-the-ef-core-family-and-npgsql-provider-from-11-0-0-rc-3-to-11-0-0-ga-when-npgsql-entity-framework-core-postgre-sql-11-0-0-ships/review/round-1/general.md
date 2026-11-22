# Round 1 — general
**Date:** 2026-11-22
**Scope reviewed:** `git diff master...HEAD` — `Directory.Packages.props`, `.config/dotnet-tools.json`, and the task kitchen record

## Summary

The change moves all seven EF Core family pins (Microsoft.AspNetCore.DataProtection.EntityFrameworkCore,
Microsoft.EntityFrameworkCore, .Design, .InMemory, .Tools,
Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore, Npgsql.EntityFrameworkCore.PostgreSQL)
from rc.3 to 11.0.0 together. It also moves the local `dotnet-ef` tool to 11.0.0. The rc.3 hold
comment was replaced by one line saying the family moves with the Npgsql provider, and the
header comment was updated to match. The Roslyn-lift comment and the
Microsoft.CodeAnalysis.Workspaces.MSBuild / CSharp.Workspaces 5.12.0 arrangement were kept, as
the task asks. The risk is low.

Checks:

- Goal restatement vs task: it matches. The `dotnet-ef` tool move is an addition, but it is
  consistent with the task (492 left the tool on rc.3 only to match this family). It is not a misread.
- No rc.3 EF version remains outside `artifacts/` and `kanban/`, apart from the generated migration
  `ProductVersion` annotations. The restatement says to leave those as history; EF rewrites the
  snapshot annotation on the next `migrations add`.
- The Roslyn-lift comment ("rc.3 and 11.0.0 GA both take … 5.9.0") is still accurate.
- `<Version>` was not bumped. This is a pins-only template change, and `check-version` passes as recorded.
- The recorded gates cover the change: `dev build` 0/0, `template-smoke --matrix` SUCCEEDED
  (includes SmokeNoPostgres), `check-version`, `ganda nuget outdated` 0, and `ganda repo audit` exit 0.
  The two advisory warnings are unrelated. `dev test` was deferred to CI with a stated reason.

No targeted command was run.

## Issues

None.
