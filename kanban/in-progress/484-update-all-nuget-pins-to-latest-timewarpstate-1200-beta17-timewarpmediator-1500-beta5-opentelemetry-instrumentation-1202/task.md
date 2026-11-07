# Update all NuGet pins to latest

## Description

`ganda nuget outdated` on 2026-11-06 reports 11 outdated pins in `Directory.Packages.props`, all patch-level:

| Package | Current | Latest |
|---------|---------|--------|
| TimeWarp.State, .Blazor, .Plus, .Telemetry, .DevTools.Export, .DevTools.Hosting, .DevTools.Wasm | 12.0.0-beta.16 | 12.0.0-beta.17 |
| TimeWarp.Mediator, TimeWarp.Mediator.Generators | 15.0.0-beta.4 | 15.0.0-beta.5 |
| OpenTelemetry.Instrumentation.AspNetCore, OpenTelemetry.Instrumentation.Http | 1.20.1 | 1.20.2 |

Bump every one to the latest listed, then re-run `ganda nuget outdated` and bump anything else it reports (the
Aspire CLI must still match `<AspireVersion>`, `tw-deploy`). Do not backward-pin. Keep the FastEndpoints comment
and other pin comments accurate.

What is new in these releases and what to check:

- TimeWarp.State 12.0.0-beta.17 (release note in the timewarp-state repo, `documentation/release-notes/release12.0.0-beta.17.md`):
  one State graph with golden-parity emitters, `[StateManifest]` assembly attribute, analyzers TWS0013
  (JsonSerializerContext) and TWS0014 (`[Reducer]`), `[Reducer]` generated handlers, generator diagnostic TWSG003 on
  record and struct shapes that were previously ignored, and the DevTools manifest ingest. Expect new analyzer
  diagnostics in this repo's State code; fix each one at the source (for TWS0013 the code fix adds the state to the
  context). Do not suppress. Do not migrate actions to `[Reducer]` here; that is task 483.
- TimeWarp.Mediator 15.0.0-beta.5: adds `GeneratedHandlerAttribute`; no breaking change expected.
- OpenTelemetry instrumentation 1.20.2: patch.

## Requirements

- All pins in the table at latest; `ganda nuget outdated` reports 0 outdated.
- `dev build` 0 warnings 0 errors; `dev template-smoke` passes (package bumps change template output).
- `dev check-version`: bump `<Version>` in `source/Directory.Build.props` and the platform package pins that equal it
  in the same commit (task 124 policy). 2.0.0-beta.32 is unreleased per `dev check-version`; if it still reports
  beta.32 as safe to release, no further bump is needed, state that in Results.
- Any new State diagnostic fixed at the source, listed in Results with the rule id and the fix.
- `ganda repo audit` clean.

## Goal (implementer restatement)

Move every outdated NuGet pin in `Directory.Packages.props` to its latest version. That is TimeWarp.State
12.0.0-beta.17 (7 packages), TimeWarp.Mediator 15.0.0-beta.5 (2), OpenTelemetry instrumentation 1.20.2 (2), and
anything else a re-run of `ganda nuget outdated` reports. Keep the Aspire CLI in step with `<AspireVersion>`, never
backward-pin, and keep pin comments accurate. Fix any new State analyzer or generator diagnostic (TWS0013,
TWS0014, TWSG003, ...) at the source without suppressing it, and do not migrate actions to `[Reducer]` (task 483).

Acceptance:
- `ganda nuget outdated` reports 0 outdated.
- `dev build` gives 0 warnings and 0 errors, and `dev template-smoke` passes.
- `dev check-version` is satisfied. Bump `<Version>` and the matching platform pins in one commit only if beta.32 is
  no longer safe to release; otherwise say so in Results.
- Each new State diagnostic is listed in Results with its rule id and fix.
- `ganda repo audit` is clean.

## Checklist

- [x] Pins bumped; `ganda nuget outdated` clean
- [x] New State analyzer diagnostics fixed at the source, listed in Results
- [x] `dev build` 0/0; `dev template-smoke`; `dev check-version`; audit clean

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-06)
- 2026-11-07 implementer (ganda task work, Claude Opus 5.5): pins bumped, TWS0013 fixed at the source, gates run.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 2725c6bb-bd34-45c9-b1b4-9a003d8ab39c, max-turns 500, Done — 2026-11-07T07:44:10Z

## Results

### Pins

`ganda nuget outdated -u -f`, then a re-run reports "All packages are up to date". The tool added a UTF-8 BOM to
`Directory.Packages.props`, which I removed.

- TimeWarp.State, .Blazor, .Plus, .Telemetry, .DevTools.Export, .DevTools.Hosting, .DevTools.Wasm: 12.0.0-beta.16 →
  12.0.0-beta.17
- TimeWarp.Mediator, TimeWarp.Mediator.Generators: 15.0.0-beta.4 → 15.0.0-beta.5
- OpenTelemetry.Instrumentation.AspNetCore, OpenTelemetry.Instrumentation.Http: 1.20.1 → 1.20.2
- libphonenumber-csharp: 9.0.42 → 9.0.43 (the re-run's extra)

The other OpenTelemetry pins (Exporter, Extensions.Hosting, Instrumentation.Runtime) stay at 1.20.1, which is their
latest. The Aspire CLI is 13.6.4, which matches `<AspireVersion>`. The pin comments still hold. The TWA0022 test stub
comment now says Mediator 15.0.0-beta.5: `ISender`, `IMediator` and `IPublisher` have the same members as in beta.4
(checked against both packages' XML docs).

### New State diagnostics

- **TWS0013** (25 errors in web-spa, 1 in web-spa-integration-tests). Each state that telemetry snapshots serialize
  must be listed on a `JsonSerializerContext`. web-spa really does turn snapshots on (Debug, Development, State
  DevTools), so this is not noise. Fix:
  - New `source/container-apps/web/projects/web-spa/state-json-context.cs`: a public `StateJsonContext` that lists all
    25 web-spa states. `SuperheroState` is inside `#if grpc` and `WeatherForecastsState` inside `#if api`, the same
    symbols as the template.json excludes for their folders.
  - Both DevTools snapshot registrations (web-spa `Program.AddStateDevToolsExportAsync`, web-server
    `WebServerStateDevTools.AddExport`) now resolve through
    `JsonTypeInfoResolver.Combine(StateJsonContext.Default, new DefaultJsonTypeInfoResolver())`. That is source-gen
    type info first, with reflection kept only for a state the context does not list (TimeWarp.State.Plus).
  - The test-only `NestedDispatchProbeState` gets a `NestedDispatchProbeJsonContext` next to it.
- Making the context compile surfaced two System.Text.Json source-gen errors, both fixed at the source:
  - **SYSLIB1220 / SYSLIB1030** on `CredentialId` / `PrincipalId`: the typed-id generator emitted its
    `JsonConverter` as `private`, so no source-gen context could use it. `TypedIdEmitter` now emits it as
    `public sealed` with `[EditorBrowsable(Never)]` and `<inheritdoc/>` on its overrides. The TypeConverter stays
    private. The Design region in `typed-id-source-generator.cs` records why, and the generator test asserts
    `public sealed class OrderIdJsonConverter`.
  - **SYSLIB1031**: Feedback contracts nest same-named types (`GetFeedback.Response` / `SubmitFeedback.Response`,
    `ListFeedback.Item` / `ListMyFeedback.Item`). Each one, plus its `IReadOnlyList<>`, gets an explicit
    `TypeInfoPropertyName` on the context.
- TWS0014 and TWSG003 did not fire. No action was migrated to `[Reducer]` (task 483).
- New test `tests/container-apps/web/web-spa-integration-tests/serialization/state-json-context-tests.cs` takes every
  concrete web-spa `IState` from a real store and serializes it through `StateJsonContext` alone, with no reflection
  fallback. It passes.

### Gates

- `dev build`: 0 warnings, 0 errors.
- `dev test --group fast`: 14/14 projects passed. That includes timewarp-architecture-sourcegenerator-tests and
  analyzers-tests.
- web-spa-integration-tests (`dotnet test -c Release`): 579/579, including the new snapshot test. The first two runs
  failed one test, `Update_WeatherForecastState_With_WeatherForecasts_From_Server`, for an environmental reason: the
  ingress → api-server TLS handshake failed because the current HTTPS dev certificate (2674D1…, created 2026-11-06)
  had never been trusted on this machine. `dotnet dev-certs https --trust` (user-level) fixed that, and the rerun
  passes.
- `dev template-smoke` (default): passed after the dev-cert trust (web 464/464, api 9/9, testing 14/14, co-located
  5/5, 5/5, 2/2). The first run failed 18 HTTPS tests on `UntrustedRoot` for the same certificate reason.
- `dev template-smoke --matrix`: "Template smoke SUCCEEDED" for all six variants (Default, NoPostgres, NoApi, NoWeb,
  NoGrpc, NoYarp), with 0 failed tests. This covers the new `#if grpc` / `#if api` lines in `StateJsonContext`.
- `dev check-version`: source 2.0.0-beta.32 vs NuGet 2.0.0-beta.31, "safe to release". No further version bump needed.
- `ganda repo audit`: exit 0. Boy-scout fix: `vscode-settings-gitignore` was an Error, because `.vscode/settings.json`
  was tracked with per-machine Peacock colors. `ganda repo audit --fix` removed the `!.vscode/settings.json`
  re-include from `.gitignore` and untracked the file. Remaining warnings predate this task: kanban-landed stale
  claims and source-file-line-cap on EF migration designers.

### How to validate

Smoke:

```bash
ganda nuget outdated
./bin/dev build
(cd tests/container-apps/web/web-spa-integration-tests && dotnet build -c Release && dotnet test -c Release --no-build)
./bin/dev template-smoke
ganda repo audit
```

Expect: `ganda nuget outdated` prints "All packages are up to date"; `dev build` reports 0 Warning(s) 0 Error(s); the
web-spa integration run includes `Serialize_Every_State_Without_Reflection` passing (the Aspire HTTPS facts need a trusted
HTTPS dev certificate); template smoke ends without "Template smoke FAILED"; audit exits 0.

## Notes

Steve 2026-11-06: TimeWarp.State 12.0.0-beta.17 released; update all NuGets. Task 483 (`[ContractState]`, option 4)
depends on this bump landing first.
