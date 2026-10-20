# Round 1 — general
**Date:** 2026-10-20
**Scope reviewed:** same as framework

## Summary

Version-only change: 14 packages move to the versions `ganda nuget outdated` reported, the AppHost
`Aspire.AppHost.Sdk` and the CI `Aspire.Cli` install move to 13.6.2 with the hosting packages, and
Microsoft.OpenApi moves 2.12.2 -> 2.13.0 (latest 2.x) instead of 3.11.0 because
FastEndpoints.OpenApi 8.3.0 -> Microsoft.AspNetCore.OpenApi 10.0.11 caps it at `< 3.0.0`. Risk is
low. Pin comments were updated to stop hard-coding the old versions.

Re-verified in this worktree (not taken from the implementer report):

- `ganda nuget outdated`: 1 outdated package, Microsoft.OpenApi 2.13.0 -> 3.x (major).
- `obj/project.assets.json`: aspire-app-host resolves Aspire.Hosting.AppHost 13.6.2,
  Aspire.Hosting.Yarp 13.6.2, Kubernetes and EntityFrameworkCore 13.6.2-preview.1.26519.11;
  foundation-server resolves Microsoft.OpenApi 2.13.0. Both restore logs are empty (no NU1608).
- `git grep 13.6.1 -- ':!kanban'` is empty; no leftover 4.0.0-beta.3 / 2.18.0 / 1.4.1 pins.
  OpenTelemetry.Instrumentation.* stay 1.20.0; outdated reports them current.
- The task 257 note is a correct hand-off for the 3.x path.

`dev build`, `dev test`, and `dev template-smoke` were not re-run at effort 1; the restore evidence
above backs the build claim, and the remaining gate results are the implementer's report.

Goal restatement matches the task. The restatement said OpenApi "stays at 2.12.2" if 3.x is
blocked; the implementation moved within 2.x to 2.13.0, which fits the task description ("latest
as far as the build allows") better. This is not a misread.

## Issues

### Issue 1 — Severity: nit
- File: Directory.Packages.props:135
- Description: The task 201 paragraph still says "Stay on 2.12.2 until a .NET version ships…". The
  live pin is now 2.13.0. Line 130 and the task 392 entry (line 152) give the current pin, but this
  sentence still reads as an instruction.
- Suggestion: Say "Stay on 2.x until …" when this block is next edited.
- Status: open
