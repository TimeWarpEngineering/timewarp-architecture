# Update NuGet packages to latest (ganda nuget outdated)

## Description

Bring every package `ganda nuget outdated` reports to its latest version, as far as the build and
tests allow. On 2026-10-19 it reported 29 outdated packages:

- Microsoft.Extensions.* (AI, AI.OpenAI, Compliance.Redaction, Http.Resilience, ServiceDiscovery,
  Telemetry, Diagnostics.Testing) 10.10 -> 10.11
- EF Core and Npgsql EF Core 11.0.0-rc.x -> 11.0.0-rc.2; Microsoft.Extensions.Diagnostics 11 rc bump
- OpenTelemetry exporter, hosting and instrumentation 1.19 -> 1.20.0
- TimeWarp.Mediator.Contracts / Generators / Analyzers 14.0.0-beta -> 14.0.0 (stable)
- Microsoft.FluentUI.AspNetCore.* 5.0.0 -> 5.0.1
- Blazilla 2.4.2, Markdig 1.4.1
- Microsoft.OpenApi 2.12.2 -> 3.10.2 (major)
- System.Security.Cryptography.Pkcs 9.0.21 -> 10.0.13 (major)
- TimeWarp.Nuru 3.0.0 -> 4.0.0-beta.2 (flagged by `ganda repo audit`, which blocks signing; includes `#:package` runfile directives such as tools/dev-cli)

## Checklist

- [ ] Run `ganda nuget outdated --update --force` in the task worktree
- [ ] `dev build` is 0 warnings, 0 errors; fix breaks forward (never backward-pin TimeWarp packages)
- [ ] If a package cannot move (for example Microsoft.OpenApi 3 conflicts with the version
      Microsoft.AspNetCore.OpenApi requires), keep the current version and record the evidence in Notes
- [ ] Tests pass (`dev test` or the repo's test lanes)
- [ ] Bump `<Version>` and the platform package pins in the same commit; `dev check-version` passes
- [ ] `dev template-smoke` passes
- [ ] `ganda repo audit` is clean

## Session

- Created: 2603262 (2026-10-19)

## Notes

Requested by Steve 2026-10-19: "create a update task and run ganda nuget outdated and update everything that we can."
