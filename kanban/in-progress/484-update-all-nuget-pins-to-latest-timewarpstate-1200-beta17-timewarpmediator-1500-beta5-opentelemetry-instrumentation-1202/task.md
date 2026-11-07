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

## Checklist

- [ ] Pins bumped; `ganda nuget outdated` clean
- [ ] New State analyzer diagnostics fixed at the source, listed in Results
- [ ] `dev build` 0/0; `dev template-smoke`; `dev check-version`; audit clean

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-06)

## Notes

Steve 2026-11-06: TimeWarp.State 12.0.0-beta.17 released; update all NuGets. Task 483 (`[ContractState]`, option 4)
depends on this bump landing first.
