# Move to .NET 11 GA and bring all NuGet pins current

## Description

Architecture is the last TimeWarp repo on the .NET 11 RC3 toolchain. `global.json` pins
`11.0.100-rc.3.26522.148`; every Microsoft.* pin is `11.0.0-rc.3.26522.148` while 11.0.0 GA is
out. The libraries we depend on have moved: TimeWarp.Mediator 15.0.0-beta.9 and the coming
TimeWarp.State 12.0.0-beta.18 compile their generators and analyzers against Roslyn 5.12.0 and
load only in the .NET 11 GA SDK compiler. Mediator already did this swap (timewarp-mediator task
023-001, commit b8432b5) and State did too (timewarp-state tasks 156 and 157, commits b4a19510 and
e4c304a8); read those diffs for the shape.

`ganda nuget outdated` on 2026-11-16 lists 62 outdated pins. Groups:

| Group | From | To |
|-------|------|----|
| SDK (`global.json`) | 11.0.100-rc.3.26522.148 | 11.0.100, `rollForward: latestFeature` |
| Microsoft.AspNetCore.*, Microsoft.EntityFrameworkCore.*, Microsoft.Extensions.* (DI, Http, Caching, Options, Logging), System.* | 11.0.0-rc.3.26522.148 | 11.0.0 |
| Microsoft.Extensions.{AI, AI.OpenAI, Telemetry, Telemetry.Abstractions, Compliance.Redaction, Http.Resilience, ServiceDiscovery, ServiceDiscovery.Yarp, Diagnostics.Testing} | 10.11.0 | 11.0.0 |
| Microsoft.CodeAnalysis.* (CSharp, Workspaces, CodeStyle, Analyzers) | 5.9.0 | 5.12.0 |
| Microsoft.CodeAnalysis.NetAnalyzers | 11.0.100-rc.3 | 11.0.100 |
| TimeWarp.Mediator, TimeWarp.Mediator.Generators | 15.0.0-beta.5 | 15.0.0-beta.9 (net11.0 only; dispatcher unchanged, no API change) |
| TimeWarp.Contracts | 2.0.0-beta.31 | 2.0.0-beta.33 (own version line; read its release notes) |
| TimeWarp.SourceGenerators | 1.0.0-beta.13 | 1.0.0-beta.16 |
| TimeWarp.Jaribu, TimeWarp.Jaribu.TestingPlatform | 1.0.0-beta.15 | 1.0.0-beta.16 |
| TimeWarp.Nuru, TimeWarp.Nuru.DevCli | 4.0.0-beta.12 | 4.0.0-beta.14 |
| TimeWarp.Amuru, TimeWarp.Amuru.Tools | 3.0.0-beta.2 | 3.0.0-beta.5 |
| TimeWarp.Terminal | 2.0.0-beta.1 | 2.0.0-beta.2 |
| TimeWarp.Build.Tasks | 1.0.0 | 2.0.0 (major; net11.0 only; `TWBT0001` on .NET Framework MSBuild, see Mediator beta.8 notes) |
| TimeWarp.Multiavatar | 1.0.0-beta.13 | 1.0.0-beta.15 |
| FastEndpoints, FastEndpoints.OpenApi | 9.0.0-beta.7 | 9.0.0 (GA; rewrite the pin comment that explains the beta pin) |
| Microsoft.AspNetCore.OpenApi | 11.0.0-rc.3 | 11.0.0 |
| Scalar.AspNetCore | 2.19.0 | 2.21.0 |
| Blazilla | 2.4.3 | 3.0.1 (major; read its release notes, fix every EditForm validator site: RoleForm, TodoItemForm, ProfilePage, AuthenticationPage) |
| OpenTelemetry.* (5 packages) | 1.20.x | 1.21.0 |
| timewarp-simple-icons | 16.36.0 | 16.37.0 |
| System.Security.Cryptography.Pkcs | 10.0.14 | 11.0.0 |

TimeWarp.State stays at 12.0.0-beta.17; it is the latest on NuGet. When beta.18 ships it is a
follow-up pin bump, not this task.

## Requirements

- Every row above at the listed version; `ganda nuget outdated` reports 0 outdated afterwards.
  Anything new it reports gets bumped too. Never backward-pin; if a bump breaks, fix forward or
  stop and report in `## Notes`.
- `global.json` on 11.0.100 GA. CI runs on the SDK `global.json` names; confirm `workflow.yml`
  and `dev workflow` need no change, or change them with the pin.
- Aspire: `<AspireVersion>` and the Aspire CLI must agree (`tw-deploy`); bump only if outdated
  lists it.
- Majors: Blazilla 3.0.1 and TimeWarp.Build.Tasks 2.0.0. Read their release notes before bumping
  and list every code change they forced in Results. Audit `--fix` output is policy and rides
  along.
- `dev build` 0 warnings 0 errors. New analyzer diagnostics from the newer Roslyn, NetAnalyzers
  11.0.100, SourceGenerators beta.16 or Mediator beta.9 are fixed at the source, never suppressed;
  list each rule id and fix in Results.
- `dev test`, `dev template-smoke --matrix` (SDK and package changes alter every template
  variant), Playwright, and `dev check-version` pass. `dev check-version`: 2.0.0-beta.32 is
  unreleased; bump `<Version>` and the platform pins that equal it only if it says so.
- `ganda repo audit` clean.
- Do not open tasks or PRs in other repos. If a TimeWarp package needs a fix upstream, stop and
  report.

## Checklist

- [ ] SDK and Microsoft.* pins on GA; Roslyn 5.12.0; CI unaffected or updated
- [ ] TimeWarp pins current (Mediator beta.9, Contracts beta.33, SourceGenerators, Jaribu, Nuru, Amuru, Terminal, Build.Tasks 2, Multiavatar)
- [ ] Third-party pins current (FastEndpoints 9 GA with comment rewrite, Blazilla 3 with validator fixes, Scalar, OpenTelemetry, icons)
- [ ] `ganda nuget outdated` 0; build 0/0; tests; template-smoke --matrix; Playwright; check-version; audit clean
- [ ] Results list every forced code change and every new diagnostic fixed

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-16)

## Notes

Steve 2026-11-16: "review mediator and state, as both have had many commits and this repo is
behind" → survey found no library-surface changes since the current pins, only the toolchain
move to .NET 11 GA and Roslyn 5.12.0. Task 483 (`[ContractState]`) stays blocked on its own
design question; nothing here changes it. Architecture 2.0.0-beta.32 releases after this lands.
