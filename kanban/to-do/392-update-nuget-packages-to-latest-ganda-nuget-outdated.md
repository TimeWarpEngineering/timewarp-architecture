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

## Checklist

- [ ] Run `ganda nuget outdated --update --force` in the task worktree
- [ ] Keep Aspire in step: the AppHost `Aspire.AppHost.Sdk` version and any Aspire pins not in CPM
      move to 13.6.2 with the packages
- [ ] Microsoft.OpenApi: task 389 kept 2.12.2 because FastEndpoints.OpenApi depends on
      Microsoft.AspNetCore.OpenApi, which requires `Microsoft.OpenApi [2.7.5, 3.0.0)`. Re-check with
      the current FastEndpoints.OpenApi; move only if restore and build pass, else keep 2.12.2 and
      record the evidence in Notes
- [ ] `dev build` is 0 warnings, 0 errors; fix breaks forward (never backward-pin TimeWarp packages)
- [ ] Tests pass (`dev test` or the repo's test lanes)
- [ ] `dev check-version` passes; bump `<Version>` and the platform pins in the same commit only if
      the current version is already on NuGet
- [ ] `dev template-smoke` passes
- [ ] `vally lint skills` and `ganda repo audit` are clean

## Session

- Created: 2026-10-20

## Notes

Requested by Steve 2026-10-20: "let's create a task to update Nugets run `ganda nuget outdated`".
Previous run: task 389 (2.0.0-beta.22).
