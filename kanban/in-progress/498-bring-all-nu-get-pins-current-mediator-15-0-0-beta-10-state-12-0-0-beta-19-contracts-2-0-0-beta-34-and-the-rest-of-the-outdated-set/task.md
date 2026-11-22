# Bring all NuGet pins current: Mediator 15.0.0-beta.10, State 12.0.0-beta.19, Contracts 2.0.0-beta.34 and the rest of the outdated set

## Description

Since task 492 (2026-11-16) the upstream packages moved. Bring every pin in
`Directory.Packages.props` to the current version `ganda nuget outdated` reports, except the
EF Core family and the Npgsql provider, which task 494 moves in the same work-set before this
task runs. Never backward-pin.

### Pins to move (from `ganda nuget outdated`, 2026-11-22)

| Package | From | To |
|---|---|---|
| TimeWarp.Mediator, TimeWarp.Mediator.Generators | 15.0.0-beta.9 | 15.0.0-beta.10 |
| TimeWarp.State, .Blazor, .Plus, .Telemetry, .DevTools.Export, .DevTools.Hosting, .DevTools.Wasm | 12.0.0-beta.17 | 12.0.0-beta.19 |
| TimeWarp.Contracts | 2.0.0-beta.33 | 2.0.0-beta.34 |
| TimeWarp.SourceGenerators | 1.0.0-beta.16 | 1.0.0-beta.17 |
| TimeWarp.Jaribu, TimeWarp.Jaribu.TestingPlatform | 1.0.0-beta.16 | 1.0.0-beta.18 |
| TimeWarp.Nuru, TimeWarp.Nuru.DevCli | 4.0.0-beta.15 | 4.0.0-beta.16 |
| TimeWarp.Amuru, TimeWarp.Amuru.Tools | 3.0.0-beta.5 | 3.0.0-beta.10 |
| Microsoft.Extensions.* (Telemetry.Abstractions, ServiceDiscovery.Yarp, AI, AI.OpenAI, Compliance.Redaction, Http.Resilience, ServiceDiscovery, Telemetry, Diagnostics.Testing) | 11.0.0 | 11.0.1 |
| Microsoft.OpenApi | 3.11.0 | 3.11.1 (confirm it still satisfies Microsoft.AspNetCore.OpenApi's range; the pin comment says [3.11.0, 4.0.0)) |
| Scalar.AspNetCore | 2.21.0 | 3.1.1 (major: read the release notes, fix call sites) |
| Microsoft.FluentUI.AspNetCore.Components, .Icons | 5.0.1 | 5.1.0 |
| Microsoft.Playwright | 1.64.1 | 1.65.1 (keep the Playwright browser install in step) |
| Azure.Storage.Blobs | 12.30.1 | 12.30.2 |
| Testcontainers.PostgreSql | 4.16.0 | 4.17.0 |

If a later version is current when the task runs, use it. Lines reported as `$(AspireVersion)`
or `$(Tw…PackageId)` are property-driven and stay as they are unless `aspire update` says the
Aspire line moved; the Aspire CLI must then match `<AspireVersion>`.

### What the TimeWarp bumps bring

- Mediator beta.10: `GeneratedRequestPatternAttribute` and TWM013 (additive). No call-site change expected.
- State beta.18/19: declared actions (TWS0015, manifest origin/handler/response fields, dispatch
  for declared actions), Roslyn 5.12 floor, Mediator beta.10 pin. Read
  `documentation/release-notes/release12.0.0-beta.18.md` and `-beta.19.md` in timewarp-state;
  fix any new diagnostic at source, not by suppression. Do not adopt `[ContractState]` or
  declared actions here; that is task 483.
- Contracts beta.34: Mediator beta.10 pin only.
- SourceGenerators beta.17, Jaribu beta.18, Nuru/DevCli beta.16, Amuru beta.10: read release
  notes for any analyzer or API change and fix at source.

## Goal (implementer restatement)

Bring every central pin `ganda nuget outdated` still reports forward, and leave the seven
EF Core / Npgsql pins for task 494. The only major is Scalar.AspNetCore 3.1.1: its breaking
change is dropping net8/net9, which this repo does not target, so the existing
`MapScalarApiReference` call stays. New TimeWarp diagnostics get fixed at the source, not
suppressed. `tools/dev-cli/dev.cs` turns telemetry on with a literal service name so the
audit check passes. `<Version>` moves only if `dev check-version` refuses the unpublished
2.0.0-beta.32. Done means a clean build, the touched suites, template-smoke --matrix, the
Playwright suite on the new browser pin, and a clean `ganda repo audit`.

## Checklist

- [x] All pins above moved; `ganda nuget outdated` reports 0 except the seven EF Core / Npgsql pins, which task 494 moves on its own branch (do not touch them here)
- [x] Scalar 3.x and any other major: release notes read, call sites fixed, note in the pin comment
- [ ] `dev build` 0 warnings / 0 errors; new diagnostics fixed at source
- [ ] `dev test`
- [ ] `dev template-smoke --matrix`
- [ ] Playwright suite passes with the new Playwright pin
- [ ] `dev check-version`: bump `<Version>` to 2.0.0-beta.33 and the platform package pins in the same commit (library pins change, so a release follows; 2.0.0-beta.32 was never cut, so the bump may be to beta.32 if `check-version` accepts it; follow its output)
- [x] Boy-scout (audit `dev-cli-telemetry`, same fix task 485 asks for): add `.UseTelemetry(options => options.ServiceName = "timewarp-architecture-dev")` to the `NuruApp.CreateBuilder()` chain in `tools/dev-cli/dev.cs`; the API is on Nuru 4.0.0-beta.16, which this task pins. Note it under Results so 485 can close as covered
- [ ] `ganda repo audit` clean. Master is red today on `package-pin-currency`, `dev-cli-telemetry`, and `kanban-landed`; this task fixes the first two and the cockpit reconciled the third on 2026-11-22. Do not treat a check another open task owns as a blocker; report it under Results instead
- [x] No cross-repo work; stop and report if an upstream package needs a fix

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-22)
- 2026-11-22 implement oracle: pins moved; AMURU0001 and JARIBU1002 fixed at source; parked
  Blocked on the Aspire 17 move (see Blocker below).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session e8fcd1ae-74e9-4b97-a2a5-2067a155e974, max-turns 500, Blocked — 2026-11-22T07:00:50Z

## Blocker: State 12.0.0-beta.19 needs Aspire 17

`TimeWarp.State.DevTools.Hosting` 12.0.0-beta.19 depends on `Aspire.Hosting` and
`Aspire.Hosting.Dotnet` >= 17.0.0. There is no beta.18 of that package. With
`<AspireVersion>` on 13.6.5 the AppHost resolves Aspire.Hosting 17.0.0 transitively. Then
`AddProject<T>` is obsolete (CS0618, four calls in `program.cs` and `ingress-resource.cs`).

Aspire 17.0.0 is on nuget.org for the whole family (Kubernetes is stable 17.0.0; EF hosting is
17.0.0-preview.1.26567.5). A trial with `<AspireVersion>17.0.0</AspireVersion>` reports:

- ASPIRE013 (error, cannot be suppressed): a C# AppHost requires the Aspire CLI bundle, so
  `AspireUseCliBundle=false` (kept on purpose by tasks 201, 209, 232, 262, 285) must go.
  Every dev machine and CI test lane then needs Aspire CLI 17 installed.
- ASPIRE012: `Aspire.AppHost.Sdk` is obsolete; the AppHost moves to `Microsoft.NET.Sdk`.
- CS0618: `AddProject<T>` → `AddDotnetProject(name, path)` from `Aspire.Hosting.Dotnet`, so
  `IResourceBuilder<ProjectResource>` call sites in six AppHost files and the TWA0007 analyzer
  (`aspire-resource-name-analyzer.cs`, which matches `AddProject`) change too.

That is an Aspire 13.6 → 17 migration, not a pin bump, and it reverses a standing repo choice,
so this task does not take it without a decision. With `TimeWarp.State.DevTools.Hosting`
held at beta.17 on a scratch build (not committed), `./bin/dev build` is 0 warnings / 0 errors,
so every other pin on this branch builds.

Unblocks this walk: an Aspire 17 migration lands first (its own task), or Steve decides this
task takes it. Holding DevTools.Hosting at beta.17 is a backward pin and is not an option.

## Results

### What changed

- Pins: every row in the table above, at the versions listed (`ganda nuget outdated` on
  2026-11-22 reports only the seven EF Core / Npgsql pins task 494 owns). `<AspireVersion>`
  stays 13.6.5; see the blocker.
- Scalar.AspNetCore 3.1.1: 3.0.0 drops net8.0/net9.0. The `MapScalarApiReference` options used
  here are unchanged; note in the pin comment.
- `tools/dev-cli/dev.cs`: `.UseTelemetry(options => options.ServiceName = "timewarp-architecture-dev")`
  (audit `dev-cli-telemetry`; task 485 can close as covered).
- Amuru 3.0.0-beta.10 ships AMURU0001 (error): `Shell.Builder("dotnet")` with a verb that has a
  typed builder. Sixteen dev-cli calls moved to `DotNet.New().Install/Uninstall`,
  `DotNet.Run().WithFile`, `DotNet.Test()`, `DotNet.Build()`, `DotNet.Tool().Restore()/Run("dotnet-ef")`,
  and `DotNet.UserSecrets().List()` (a linked 20 s token replaces `WithTimeout`, which that
  builder lacks). `DotNetTestArguments.Test` became `PlatformArguments` (only the MTP arguments
  after `--`), `DotNetTestArguments.Build` is gone, `AspirePublishTargets.SafetySuiteTestArguments`
  became `SafetySuiteFilterArguments`, and `DbEf.BuildAddMigrationArguments` drops the leading
  `ef` (the tool is `DbEf.EfToolCommand`). Tests updated to match.
- Jaribu 1.0.0-beta.18 ships JARIBU1002: the permanently skipped `RunForever` manual-debug test
  in `web-test-server-application-tests.cs` reached no assertion; removed.

### Gates

- `dotnet run --file tools/dev-cli/dev.cs -- self-install`: pass (dev-cli compiles clean).
- `./bin/dev build`: FAIL on this branch: CS0618 `AddProject<T>` ×4 (Aspire.Hosting 17 via
  State.DevTools.Hosting beta.19). With DevTools.Hosting at beta.17 on a scratch build: pass,
  0 warnings / 0 errors.
- `tests/tools/dev-cli-tests/dotnet-test-arguments-tests.cs`, `aspire-publish-targets-tests.cs`,
  `db-ef-tests.cs`: not standalone runfiles (no `JARIBU_MULTI` guard; `dev-cli-tests.csproj`);
  compiled in the scratch build; CI runs them.
- `tests/container-apps/web/web-server-integration-tests/.../web-test-server-application-tests.cs`:
  not a standalone runfile; compiled in the scratch build; CI runs it.
- `ganda repo audit`: not run (work not finished; parked Blocked).
- `dev check-version`, template-smoke --matrix, Playwright: not run (parked Blocked).

### How to validate

- Smoke: after the Aspire 17 decision lands, `./bin/dev build`, then
  `./bin/dev template-smoke --matrix`, then `./bin/dev test --group playwright`.
- Expect: build 0 warnings / 0 errors; `ganda nuget outdated` lists only the seven EF Core /
  Npgsql pins; `ganda repo audit` passes `package-pin-currency` and `dev-cli-telemetry`.

## Notes

- Filed per Steve 2026-11-22 ("file the task and lets do the updates"). Order changed the same day:
  this task runs first and alone, because the walk's audit gate refuses while master is red on
  pin currency and dev-cli telemetry; 494 (whose pins are already moved on its branch) resumes
  after this lands.
- Unblocks task 483 (needs Mediator beta.10 and State beta.19 pinned).

## Blocked

2026-11-22: TimeWarp.State.DevTools.Hosting 12.0.0-beta.19 needs Aspire.Hosting 17.0.0. Aspire 13.6.5 → 17 is a migration: the AppHost must use the Aspire CLI bundle (ASPIRE013, which reverses the AspireUseCliBundle=false choice kept by tasks 201, 209, 232, 262 and 285), Aspire.AppHost.Sdk becomes Microsoft.NET.Sdk (ASPIRE012), and AddProject becomes AddDotnetProject, including the TWA0007 analyzer. Unblock: an Aspire 17 migration task lands first, or Steve decides task 498 takes it on.
