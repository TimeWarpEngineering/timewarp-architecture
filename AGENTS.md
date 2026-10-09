# AGENTS.md

Guidance for coding agents in this repository. `CLAUDE.md` includes this file (`@AGENTS.md`).

This repository is the `dotnet new timewarp-architecture` template. Root `source/` and `tests/`
are the template content (`.template.config/`). `timewarp-templates/` packs that content as a
NuGet template. A change here ships to every generated app. This file is the monorepo agent
guide and is not in the pack. Generated apps get `skills/`.

Feature flags `api`, `grpc`, `web`, `yarp`, and `postgres` are preprocessor switches. Leave
`<!--#if (flag)-->` and `#if flag` regions intact (TWA0008 / TWA0010). Counter and analytics
ship in every app. Removing one is `tw-slice-isolation`.

## Hard rules

- Never give calendar estimates (hours, days, weeks, sprints). Say scope, dependencies, and
  proof gates. Do not invent token or agent-hour budgets. Kanban tasks have no Estimate field
  (`tw-kanban`).
- `dev build` is 0 warnings and 0 errors. Warnings are errors.
- Tests are Jaribu and Shouldly. Do not add Fixie, xUnit, NUnit, MSTest, FluentAssertions,
  MediatR, or Tailwind.
- Mediator calls use TimeWarp.Mediator: `IRequest<OneOf<Response, SharedProblemDetails>>`.
- Purpose regions are analyzer **TWA0004** (every source file; generated code is exempt). The
  cross-repo skill's id **TWPA0004** is wrong for this repo. Maintenance rule:
  `tw-agent-context-regions`.
- When two things must agree, generate one from the other or add a build check.
- Work that changes this repo is a kanban task (`tw-kanban`). Do not hand-number task files.

## Stack

.NET 11 (SDK pin is `global.json`), C# latest, nullable on. Blazor WebAssembly and
TimeWarp.State. FastEndpoints generated from contracts (`tw-web-api-contracts`). FluentUI v5
and plain CSS (`tw-blazor-css-strategy`). Aspire. EF Core when `postgres` is on.

## Where to look

| Question | Home |
|----------|------|
| Build, run, test, database | `dev --capabilities` and `tw-dev-cli`. `dev run` starts Aspire. |
| Before a PR | `tw-pr`. Gates in this repo: `ganda repo audit`; `dev check-version` when shipping the template or packages; `dev build`; `dev template-smoke` when template output changes. Branch and merge: `tw-git`. |
| File and type names | `tw-csharp`. `.cs` kebab is **TW0001** (warning, so the build fails). Feature filenames: `tw-feature-placement`. |
| Where a file goes | `tw-feature-placement`. Slice boundaries: `tw-slice-isolation`. |
| Contracts and browser-protocol endpoints | `tw-web-api-contracts` |
| Razor, actions, outcomes | `tw-blazor`, `tw-blazor-layout` |
| Aggregates | `tw-aggregate-pattern` |
| Tests | `tw-jaribu` for the framework. Co-located runfiles, C-create / C-share, and host lanes: `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md` |
| Deploy | `tw-deploy` |
| Release cut | `tw-release`. Maintainer notes: `documentation/developer/guides/` (not packed). |
| Diagnostics TWA / TWE / SG | The build message, plus `AnalyzerReleases.Unshipped.md` under `source/analyzers/timewarp-architecture-convention-analyzers/` and `source/analyzers/timewarp-architecture-analyzers/`. Retired ids are commented there. |
| Platform NuGet vs project reference | Root `Directory.Build.props`, `Directory.Packages.props`, and `msbuild/timewarp-platform-packages.props`. |

Repo skills under `skills/` ship in generated apps. `skills/*/analysis/` does not.
`documentation/` is maintainer-only and is not packed. A live rule belongs in the skill that
owns it or in a Design region.
