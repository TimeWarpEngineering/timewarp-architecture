# Update NuGet packages to latest (ganda nuget outdated)

## Goal (implementer restatement)

Bring every package `ganda nuget outdated` lists to the newest version the build and tests still
accept. Fix compile and test breaks by moving forward. Never pin a TimeWarp package back to an
older version to make the build pass. When a package cannot move because a dependency requires the
older version, leave that pin and write the conflict evidence under Notes. Ship one version bump of
this template in the same commit as the platform package pins, and prove the result with a clean
build, tests, version check, template smoke, and repo audit.

Acceptance:

- `ganda nuget outdated --update --force` has been run in this worktree.
- `dev build` finishes with 0 warnings and 0 errors.
- Any package that stays put has Notes evidence for why.
- The repo test lanes pass.
- `<Version>` and the platform package pins change in the same commit, and `dev check-version` passes.
- `dev template-smoke` passes.
- `ganda repo audit` is clean.

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

- [x] Run `ganda nuget outdated --update --force` in the task worktree
- [x] `dev build` is 0 warnings, 0 errors; fix breaks forward (never backward-pin TimeWarp packages)
- [x] If a package cannot move (for example Microsoft.OpenApi 3 conflicts with the version
      Microsoft.AspNetCore.OpenApi requires), keep the current version and record the evidence in Notes
- [x] Tests pass (`dev test` or the repo's test lanes)
- [x] Bump `<Version>` and the platform package pins in the same commit; `dev check-version` passes
- [x] `dev template-smoke` passes
- [x] `ganda repo audit` is clean

## Session

- Created: 2603262 (2026-10-19)
- Implementation: implementer oracle (2026-10-19). Moved this kitchen to in-progress.

## Notes

Requested by Steve 2026-10-19: "create a update task and run ganda nuget outdated and update everything that we can."

**Microsoft.OpenApi stays 2.12.2.** `ganda nuget outdated --update` wrote 3.10.2. FastEndpoints.OpenApi 8.3.0 (latest stable; the outdated run reported it current) depends on Microsoft.AspNetCore.OpenApi 10.0.11, whose nuspec is `Microsoft.OpenApi [2.7.5, 3.0.0)`. 3.10.2 is outside that range, so restore would fail NU1608 under warnings-as-errors. The pin was reverted. There is still no FastEndpoints.OpenApi stable build against the OpenApi 3 object model.

**TimeWarp.Nuru / DevCli moved 3.0.0 to 4.0.0-beta.2.** The same outdated run reported stable 3.0.0 as up to date (it does not float a stable pin onto a 4.0 prerelease). `ganda repo audit` then failed the nuru check: `current: 3.0.0, latest: 4.0.0-beta.2`, which blocks signing. Both pins moved forward. dev-cli and agent-identity-cli compiled with no source changes. `bin/dev` was self-installed again. Runfile `#:package` directives in this repo are versionless; CPM is the only version site.

**dotnet-ef** is not in central package management. It was bumped in `.config/dotnet-tools.json` to `11.0.0-rc.2.26504.105` so the tool matches the EF Core pin. Npgsql.EntityFrameworkCore.PostgreSQL 11.0.0-rc.2 requires that EF Core version exactly.

No direct `Microsoft.Extensions.Diagnostics` pin exists. The 11 RC2 diagnostics package that is pinned, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`, moved with EF Core.

## Results

`ganda nuget outdated --update --force` updated 29 central pins. One was reverted (Microsoft.OpenApi). TimeWarp.Nuru and TimeWarp.Nuru.DevCli were then set to 4.0.0-beta.2 so the audit nuru check can pass. Repo version and the twelve platform package pins are `2.0.0-beta.22` (2.0.0-beta.21 is already on NuGet).

Pins that moved:

| Package | From | To |
|---|---|---|
| Microsoft.Extensions.AI, Telemetry, Telemetry.Abstractions, Compliance.Redaction, Http.Resilience, ServiceDiscovery, ServiceDiscovery.Yarp, Diagnostics.Testing | 10.10.0 | 10.11.0 |
| Microsoft.Extensions.AI.OpenAI | 10.10.1 | 10.11.0 |
| EF Core family + HealthChecks.EntityFrameworkCore + dotnet-ef | 11.0.0-rc.1.26425.128 | 11.0.0-rc.2.26504.105 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 11.0.0-rc.1.1 | 11.0.0-rc.2 |
| OpenTelemetry exporter + hosting | 1.19.1 | 1.20.0 |
| OpenTelemetry instrumentation (AspNetCore, Http, Runtime) | 1.19.0 | 1.20.0 |
| TimeWarp.Mediator.Contracts / Generators / Analyzers | 14.0.0-beta.5 | 14.0.0 |
| Microsoft.FluentUI.AspNetCore.Components + Icons | 5.0.0 | 5.0.1 |
| Blazilla | 2.4.1 | 2.4.2 |
| Markdig | 1.4.0 | 1.4.1 |
| System.Security.Cryptography.Pkcs | 9.0.21 | 10.0.13 |
| TimeWarp.Nuru + TimeWarp.Nuru.DevCli | 3.0.0 | 4.0.0-beta.2 |

No product C# changes. Mediator 14.0.0, FluentUI 5.0.1, Pkcs 10.0.13, EF RC2, OpenTelemetry 1.20, and Nuru 4.0.0-beta.2 all compiled on the existing sources.

Gates:

- `dev build`: 0 warnings, 0 errors.
- `dev test --no-build`: 23 projects, "Tests completed successfully!".
- dev-cli-tests 256/256, agent-identity-cli-tests 11/11, after the Nuru pin.
- `dev check-version`: source 2.0.0-beta.22 is one prerelease ahead of NuGet 2.0.0-beta.21.
- `dev template-smoke`: `Template smoke SUCCEEDED` (SmokeDefault).
- `dotnet tool restore` installed dotnet-ef 11.0.0-rc.2.26504.105.
- `ganda repo audit`: 32 passed, 0 failed. The nuru check reports TimeWarp.Nuru up to date.
- `bin/dev` self-install succeeded. `--capabilities` lists 32 commands. `db nuke` without `--yes` refuses and exits 1.

### How to validate

Smoke:

```bash
./bin/dev check-version
./bin/dev build
./bin/dev test --no-build
./bin/dev template-smoke
ganda repo audit
./bin/dev db nuke
```

Expect:

- check-version prints source `2.0.0-beta.22`, latest NuGet `2.0.0-beta.21`, and "Version in source is new".
- build prints 0 Warning(s) and 0 Error(s).
- test prints "Tests completed successfully!".
- template-smoke prints `Template smoke SUCCEEDED`.
- repo audit prints "Repository passes all audit checks" and the nuru row is PASS.
- `db nuke` prints "Refusing to nuke without --yes" and exits 1.
