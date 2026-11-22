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

### Aspire 17 is in scope (Steve, 2026-11-22)

Steve decided: move to Aspire 17 in this task. We own every repo in the chain and do not pin
backward; State 12.0.0-beta.19 already made this move. Do the migration here, in the same PR:

- `<AspireVersion>` 13.6.5 -> 17.0.0 (latest 17.x on nuget.org), every `Aspire.*` pin follows the
  property as today; drop any `-preview` suffix that 17 no longer needs.
- ASPIRE013: enable the Aspire CLI bundle (remove `AspireUseCliBundle=false` and whatever tasks
  201/209/232/262/285 added to keep it off). The AppHost starts through the `aspire` CLI. Update
  `dev run` and any dev-cli code that launched the AppHost with `dotnet run`, the CI lanes that
  start it, and `tw-deploy` / `tw-dev-cli` skill text that states the old behavior. The Aspire
  CLI on dev machines and CI must be 17 (`aspire update --self`; CI installs the pinned version).
- ASPIRE012: `Aspire.AppHost.Sdk` -> `Microsoft.NET.Sdk` for the AppHost project(s), per the
  Aspire 17 migration guide (`aspire update --migrate` is the tool; use the `aspire-orchestration`
  / `aspire` skills).
- CS0618: `AddProject<T>` -> `AddDotnetProject` in all AppHost files (six found), and the TWA0007
  analyzer plus its tests move to match on `AddDotnetProject`. Keep the ServiceNames resource-name
  rule intact (names must equal web-server/api-server/grpc-server).
- Then State family to 12.0.0-beta.19 including DevTools.Hosting, as the table says.
- `dev run` must start the stack locally, and the Aspire lanes of `dev test` must pass.

### Checklist additions (Aspire 17)

- [x] `<AspireVersion>` 17.x; all Aspire pins restore
- [x] CLI bundle on; AppHost SDK swapped; `AddDotnetProject` everywhere; TWA0007 updated with tests
- [x] dev-cli, CI workflow, and skills updated for the Aspire 17 CLI (pinned version in CI)
- [x] Aspire test lanes pass; `dev template-smoke --matrix` passes. `dev run` itself is not run by
  the walk (worker sessions do not start the AppHost against the shared user secrets); it is the
  Smoke step under How to validate
- [x] Release-note / pin comment states the Aspire 17 move and its date

## Goal (implementer restatement)

Bring every central pin `ganda nuget outdated` still reports forward, and leave the seven
EF Core / Npgsql pins for task 494. The only major is Scalar.AspNetCore 3.1.1: its breaking
change is dropping net8/net9, which this repo does not target, so the existing
`MapScalarApiReference` call stays. New TimeWarp diagnostics get fixed at the source, not
suppressed. `tools/dev-cli/dev.cs` turns telemetry on with a literal service name so the
audit check passes. `<Version>` moves only if `dev check-version` refuses the unpublished
2.0.0-beta.32. Done means a clean build, the touched suites, template-smoke --matrix, the
Playwright suite on the new browser pin, and a clean `ganda repo audit`.

Aspire 17 (added after Steve's 2026-11-22 decision): State 12.0.0-beta.19 needs Aspire 17, so
`<AspireVersion>` goes to 17.0.0 with no backward pin. The AppHost becomes a `Microsoft.NET.Sdk`
project on `Aspire.Hosting.AppHost` + `Aspire.Hosting.Dotnet`, takes DCP and the dashboard from
the Aspire CLI bundle, and declares projects with `AddDotnetProject` by path. TWA0007 follows the
new method name and keeps the ServiceNames rule. dev-cli, CI and the tw-deploy skill state the
CLI 17 requirement, and CI installs the pinned CLI wherever the AppHost builds. Done adds: every
Aspire-booting suite passes on 17 (aspire-tests, api-server and web-spa integration, Playwright)
and template-smoke --matrix passes.

## Checklist

- [x] All pins above moved; `ganda nuget outdated` reports 0 except the seven EF Core / Npgsql pins, which task 494 moves on its own branch (do not touch them here)
- [x] Scalar 3.x and any other major: release notes read, call sites fixed, note in the pin comment
- [x] `dev build` 0 warnings / 0 errors; new diagnostics fixed at source
- [x] `dev test` (the suites this change touches, run one project at a time; CI runs the full `dev test`)
- [x] `dev template-smoke --matrix`
- [x] Playwright suite passes with the new Playwright pin
- [x] `dev check-version`: bump `<Version>` to 2.0.0-beta.33 and the platform package pins in the same commit (library pins change, so a release follows; 2.0.0-beta.32 was never cut, so the bump may be to beta.32 if `check-version` accepts it; follow its output)
- [x] Boy-scout (audit `dev-cli-telemetry`, same fix task 485 asks for): add `.UseTelemetry(options => options.ServiceName = "timewarp-architecture-dev")` to the `NuruApp.CreateBuilder()` chain in `tools/dev-cli/dev.cs`; the API is on Nuru 4.0.0-beta.16, which this task pins. Note it under Results so 485 can close as covered
- [x] `ganda repo audit` clean. Master is red today on `package-pin-currency`, `dev-cli-telemetry`, and `kanban-landed`; this task fixes the first two and the cockpit reconciled the third on 2026-11-22. Do not treat a check another open task owns as a blocker; report it under Results instead
- [x] No cross-repo work; stop and report if an upstream package needs a fix

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-22)
- 2026-11-22 implement oracle: pins moved; AMURU0001 and JARIBU1002 fixed at source; parked
  Blocked on the Aspire 17 move (see Blocker below).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session e8fcd1ae-74e9-4b97-a2a5-2067a155e974, max-turns 500, Blocked — 2026-11-22T07:00:50Z
- 2026-11-22 implement oracle (resumed): Aspire 17 migration done in this task; gates below.

## Blocker: State 12.0.0-beta.19 needs Aspire 17 (resolved: migrated in this task)

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

- Pins: every row in the table above, at the versions listed. `ganda nuget outdated`
  (2026-11-22, after the Aspire move) reports only the seven EF Core / Npgsql pins task 494 owns.
- Scalar.AspNetCore 3.1.1: 3.0.0 drops net8.0/net9.0. The `MapScalarApiReference` options used
  here are unchanged; note in the pin comment.
- `tools/dev-cli/dev.cs`: `.UseTelemetry(options => options.ServiceName = "timewarp-architecture-dev")`
  (audit `dev-cli-telemetry`; task 485 can close as covered).
- Amuru 3.0.0-beta.10 ships AMURU0001 (error): sixteen `Shell.Builder("dotnet")` calls in dev-cli
  moved to the typed `DotNet.*` builders; `DotNetTestArguments`, `AspirePublishTargets` and `DbEf`
  argument helpers and their tests follow.
- Jaribu 1.0.0-beta.18 ships JARIBU1002: the skipped `RunForever` debug test is removed, and the two
  `Publish_Should_AllowTheIngressHostForPasskeys` tests (aca, aca-neon) now sit inside the
  `#if web` / `#if yarp` guards instead of guarding only their bodies, so the NoWeb and NoYarp
  variants do not compile an assertion-free test (found by template-smoke --matrix).
- Aspire 17 (`<AspireVersion>` 17.0.0, comment dated 2026-11-22):
  - `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet` pinned on the property; Kubernetes drops
    its preview suffix (stable 17.0.0); EF hosting is `$(AspireVersion)-preview.1.26567.5`.
  - AppHost csproj: `Microsoft.NET.Sdk` (ASPIRE012), no `AspireUseCliBundle=false` / `NoWarn ASPIRE010`
    (ASPIRE013), no ProjectReferences to api-server, grpc-server, web-server or yarp.
  - `AddDotnetProject(name, path, options => options.LaunchProfileName = …)` in `program.cs` and
    `ingress-resource.cs`; the paths are constants in `constants.cs`; `IResourceBuilder<DotnetProjectResource>`
    in `app-host-projects.cs`, `postgres-resources.cs`, `web-server-resource.cs`.
  - TWA0007 matches `AddDotnetProject`; `aspire-resource-name-analyzer-tests.cs` uses that stub.
  - Aspire 17 rejects an empty value for a required parameter: `entra-client-id` and
    `entra-public-origin` (empty while Entra is off) are `AsOptional()`.
  - Aspire 17 caches each `EnvironmentCallbackAnnotation`'s first evaluation per instance, and
    Aspire.Hosting.EntityFrameworkCore 17.0.0-preview appends web-server's own annotation instances
    to `ef-tool-web-migrations`. `RunDatabaseUpdateOnStart` evaluated them for the tool first, so
    web-server started with no environment (Production on Kestrel's :5000) and the ingress never
    became healthy. `PostgresResources.UnshareEnvironmentCallbacks` gives the tool its own
    annotation over each same callback (Design region says when to drop it). Upstream follow-up:
    report the shared-annotation snapshot to the Aspire EF hosting package (not filed; outside
    this org).
  - Helm output wraps ConfigMap/Secret values as `{{ tpl "…" . | quote }}`; the chart reader in
    `kubernetes-publish-chart-parsing.cs` reads that as the quoted inner template.
  - dev-cli: `dev run` refuses an Aspire CLI below 17.0 on every run (`AspireRun.MinimumRunVersion`);
    the update hint adds `aspire setup`. The pins/AppHost/CI agreement check moved to
    `tools/dev-cli/services/aspire-version-sources.cs` (TWA0038 line cap on `aspire-run.cs`) and now
    requires the two AppHost packages without their own version and refuses `Aspire.AppHost.Sdk`.
  - CI (`workflow.yml`): every `ci` group and the template-smoke job install the pinned Aspire CLI.
  - `skills/tw-deploy`: Aspire CLI version section states the bundle, `aspire setup`, and the
    per-verb minimums.
- Found on this machine: a bundle an older CLI left in `~/.aspire/bundle` (DCP 0.23.6) won over the
  17 CLI's unextracted bundle, and DCP never started. `aspire setup` fixed it; tw-deploy and the
  dev-cli hint now say to run it.

### Gates

- `dotnet run --file tools/dev-cli/dev.cs -- self-install`: pass.
- `./bin/dev build`: pass, 0 warnings / 0 errors.
- `tests/analyzers/timewarp-architecture-analyzers-tests` (holds `aspire-resource-name-analyzer-tests.cs`;
  not a standalone runfile): project run, 263/263 passed.
- `tests/tools/dev-cli-tests` (holds `aspire-run-tests.cs`, `dotnet-test-arguments-tests.cs`,
  `aspire-publish-targets-tests.cs`, `db-ef-tests.cs`; not standalone runfiles): project run, 343/343 passed.
- `tests/container-apps/aspire/aspire-tests` (holds `kubernetes-publish-chart-parsing.cs`,
  `aca-neon-publish-tests.cs`, `aca-publish-secrets-tests.cs`; not standalone runfiles): project
  run, 84/84 passed; the two aca classes again after the guard move, 22/22.
- `tests/container-apps/api/api-server-integration-tests` (boots the AppHost): 2/2 passed.
- `tests/container-apps/web/web-spa-integration-tests` (boots the AppHost): 579/579 passed.
- `tests/container-apps/web/web-spa-playwright-tests` (Playwright 1.65.1): 37/37 passed.
- `web-test-server-application-tests.cs` (web-server-integration-tests; not a standalone runfile):
  not run here; CI runs it.
- `./bin/dev lint-skills`: pass (9 skills).
- `./bin/dev check-version`: pass; 2.0.0-beta.32 is new against nuget 2.0.0-beta.31, so no bump.
- `./bin/dev template-smoke --matrix`: first run failed SmokeNoWeb and SmokeNoYarp on JARIBU1002
  (the two passkey tests); after the guard move all six variants OK, SUCCEEDED.
- `./bin/dev publish kubernetes`: publish through the Aspire 17 CLI wrote the chart (its
  safety-check failure was the YAML reader, fixed above and covered by the aspire-tests run).
- `ganda repo audit`: first run failed `analyzer-suppressions` (ASPIRE010 suppressed 0x, baseline 1:
  the AppHost `NoWarn` is gone); `ganda repo audit --fix` lowered `.timewarp/suppression-baseline.json`,
  then "Repository passes" with one advisory: `source-file-line-cap` on four files this change does
  not touch (three EF migration designer/snapshot files and a done-task research runfile).

### How to validate

- Smoke: `aspire --version` is 17.0.0 (else `dotnet tool update -g Aspire.Cli --version 17.0.0`,
  then `aspire setup`); `./bin/dev build`; `./bin/dev run`, open the dashboard, and wait for
  web-server, api-server, grpc-server, ingress and web-migrations; then
  `./bin/dev template-smoke --matrix`.
- Expect: build 0 warnings / 0 errors; `dev run` brings every resource to Running/Healthy with
  web-server on its allocated port (not :5000) and web-migrations Finished; template smoke
  SUCCEEDED for all six variants; `ganda nuget outdated` lists only the seven EF Core / Npgsql pins.

## Notes

- Filed per Steve 2026-11-22 ("file the task and lets do the updates"). Order changed the same day:
  this task runs first and alone, because the walk's audit gate refuses while master is red on
  pin currency and dev-cli telemetry; 494 (whose pins are already moved on its branch) resumes
  after this lands.
- Unblocks task 483 (needs Mediator beta.10 and State beta.19 pinned).

## Blocked

2026-11-22: TimeWarp.State.DevTools.Hosting 12.0.0-beta.19 needs Aspire.Hosting 17.0.0. Aspire 13.6.5 → 17 is a migration: the AppHost must use the Aspire CLI bundle (ASPIRE013, which reverses the AspireUseCliBundle=false choice kept by tasks 201, 209, 232, 262 and 285), Aspire.AppHost.Sdk becomes Microsoft.NET.Sdk (ASPIRE012), and AddProject becomes AddDotnetProject, including the TWA0007 analyzer. Unblock: an Aspire 17 migration task lands first, or Steve decides task 498 takes it on.

2026-11-22: resolved. Steve chose to take Aspire 17 inside this task (see "Aspire 17 is in scope" in the Description). Resume the walk.
