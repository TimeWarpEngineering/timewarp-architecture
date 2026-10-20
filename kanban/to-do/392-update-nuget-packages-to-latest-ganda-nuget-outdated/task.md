# Update NuGet packages to latest (ganda nuget outdated)

## Description

Bring every package `ganda nuget outdated` reports to its latest version, as far as the build and
tests allow. On 2026-10-20 `ganda nuget outdated --update --dry-run` reported 15:

- TimeWarp.Nuru, TimeWarp.Nuru.DevCli 4.0.0-beta.3 -> 4.0.0-beta.4
- Aspire.Hosting.* (Yarp, PostgreSQL, Docker, Azure.*, Testing) 13.6.1 -> 13.6.2; Aspire.Hosting.Kubernetes
  and Aspire.Hosting.EntityFrameworkCore 13.6.1-preview -> 13.6.2-preview
- Scalar.AspNetCore 2.18.0 -> 2.18.2
- Markdig 1.4.1 -> 1.4.2
- OpenTelemetry exporter and hosting 1.20.0 -> 1.20.1
- Microsoft.OpenApi 2.12.2 -> 3.11.0 (major)

## Goal (implementer restatement)

Move every package `ganda nuget outdated` flags to its newest version, with Aspire (CPM packages,
`Aspire.AppHost.Sdk`, and any non-CPM pins) landing on the same 13.6.2 line. Microsoft.OpenApi
moves to 3.x only if FastEndpoints.OpenApi / Microsoft.AspNetCore.OpenApi now allow it; otherwise
it stays at 2.12.2 with the restore evidence in Notes.

Acceptance:

- `ganda nuget outdated` reports nothing left except packages blocked with recorded evidence.
- `dev build` 0 warnings / 0 errors; tests pass; `dev template-smoke` passes.
- `dev check-version` passes (bump `<Version>` + platform pins together only if the current
  version is already published).
- `vally lint skills` and `ganda repo audit` clean; changes committed and pushed on the task branch.

## Checklist

- [x] Run `ganda nuget outdated --update --force` in the task worktree
- [x] Keep Aspire in step: the AppHost `Aspire.AppHost.Sdk` version and any Aspire pins not in CPM
      move to 13.6.2 with the packages
- [x] Microsoft.OpenApi: task 389 kept 2.12.2 because FastEndpoints.OpenApi depends on
      Microsoft.AspNetCore.OpenApi, which requires `Microsoft.OpenApi [2.7.5, 3.0.0)`. Re-check with
      the current FastEndpoints.OpenApi; move only if restore and build pass, else keep 2.12.2 and
      record the evidence in Notes
- [x] `dev build` is 0 warnings, 0 errors; fix breaks forward (never backward-pin TimeWarp packages)
- [x] Tests pass (`dev test` or the repo's test lanes)
- [x] `dev check-version` passes; bump `<Version>` and the platform pins in the same commit only if
      the current version is already on NuGet
- [x] `dev template-smoke` passes
- [x] `vally lint skills` and `ganda repo audit` are clean

## Session

- Created: 2026-10-20
- 2026-10-20 implementer (ganda task work, claude): ran `ganda nuget outdated --update --force`
  (15 updates), moved `Aspire.AppHost.Sdk` and the CI `Aspire.Cli` tool install to 13.6.2, held
  Microsoft.OpenApi on 2.x (2.13.0), reconciled pin comments, ran all gates.

## Notes

Requested by Steve 2026-10-20: "let's create a task to update Nugets run `ganda nuget outdated`".
Previous run: task 389 (2.0.0-beta.22).

Microsoft.OpenApi evidence (2026-10-20): outdated wrote 3.11.0. FastEndpoints.OpenApi 8.3.0 is still
the latest stable; its nuspec depends on Microsoft.AspNetCore.OpenApi 10.0.11, which declares
Microsoft.OpenApi [2.7.5, 3.0.0), so 3.x is NU1608 (warning-as-error). Pin moved to 2.13.0, the
latest in that range. FastEndpoints.OpenApi 9.0.0-beta.2 adds a net11.0 group on
Microsoft.AspNetCore.OpenApi 11.0.0-rc.2.26504.105, whose range is [3.10.0, 4.0.0). That opens the
3.x path through a FastEndpoints major/prerelease move, which task 257 owns (evidence added there).

## Results

All 15 reported packages moved; Microsoft.OpenApi moved within 2.x (2.12.2 -> 2.13.0) instead of
to 3.11.0 (see Notes). Changes:

- `Directory.Packages.props`: TimeWarp.Nuru / Nuru.DevCli 4.0.0-beta.4; Aspire.Hosting.* 13.6.2
  (Kubernetes and EntityFrameworkCore 13.6.2-preview.1.26519.11); Aspire.Hosting.Testing 13.6.2;
  Scalar.AspNetCore 2.18.2; Markdig 1.4.2; OpenTelemetry exporter + hosting 1.20.1;
  Microsoft.OpenApi 2.13.0. Pin comments now name the new versions; OpenApi block records the
  task 392 re-check.
- `aspire-app-host.csproj`: `Aspire.AppHost.Sdk/13.6.2`.
- `.github/workflows/workflow.yml`: `Aspire.Cli --version 13.6.2`.
- `foundation-server.csproj`: comment no longer hard-codes 2.12.2.
- OpenTelemetry.Instrumentation.* stay 1.20.0 (not reported as outdated).
- No version bump: `dev check-version` reports source 2.0.0-beta.24, NuGet latest 2.0.0-beta.23.

Gates (2026-10-20, this worktree): `dev build` 0 warnings / 0 errors; dev CLI rebuilt on
Nuru beta.4 (`dev self-install`); `dev test` 23/23 projects passed; `dev check-version` passed;
`dev template-smoke` SUCCEEDED; `vally lint skills` 9/9; `ganda repo audit` passes all checks;
`ganda nuget outdated` now lists only Microsoft.OpenApi (major, blocked).

### How to validate

Smoke:

```bash
ganda nuget outdated
./bin/dev build
./bin/dev test
./bin/dev template-smoke
ganda repo audit
```

Expect: `ganda nuget outdated` reports exactly one outdated package, Microsoft.OpenApi 2.13.0 ->
3.x (major). `dev build` ends with 0 Warning(s) / 0 Error(s). `dev test` reports 23 of 23 projects
passed. Template smoke prints `Template smoke SUCCEEDED`. Audit prints `Repository passes all audit
checks.` `git grep 13.6.1 -- ':!kanban'` returns nothing.
