# Round 1 — general
**Date:** 2026-11-07
**Scope reviewed:** branch vs master: `Directory.Packages.props`, `.gitignore` / `.vscode/settings.json` (audit --fix),
`typed-id-emitter.cs`, `typed-id-source-generator.cs`, `web-spa/state-json-context.cs`, `web-spa/program.cs`,
`web-server/web-server-state-dev-tools.cs`, the two analyzer/generator test edits, `api-handler-nested-dispatch-tests.cs`,
and the new `serialization/state-json-context-tests.cs`.

## Summary

The change bumps the 11 listed pins and libphonenumber-csharp (the re-run's extra). It fixes TWS0013 by adding a
`StateJsonContext` that lists every web-spa state, and wires both DevTools snapshot registrations to resolve from it
first, with reflection as the fallback. The typed-id `JsonConverter` is now public, because otherwise the context hits
SYSLIB1220/SYSLIB1030. The Feedback name collisions get `TypeInfoPropertyName` (SYSLIB1031). Nothing is suppressed, and
no action moved to `[Reducer]`. The overall risk is low.

Checks I re-ran in this worktree:
- `./bin/dev build`: 0 warnings, 0 errors.
- `ganda nuget outdated`: "All packages are up to date".
- `Serialize_Every_State_Without_Reflection`: 1/1 passed.
- `TypedIdSourceGenerator` tests: 11/11 passed.

The `#if grpc` / `#if api` gating in `state-json-context.cs` matches the template.json excludes for
`features/superhero/**` (`!grpc`) and `features/weather-forecast/**` (`!api`). The implementer reported that the
`--matrix` smoke passed. I did not re-run it.

`NestedDispatchProbeJsonContext` has no consumer. It is still the analyzer's intended remedy, which is to list the state
on a context, and it generates real type info. It is not a suppression.

`.gitignore` and the untracked `.vscode/settings.json` come from `ganda repo audit --fix`, which is policy.

I found no mismatch between the Goal restatement and the task.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/state-json-context.cs:23
- Description: 13 of the 18 `using TimeWarp.Architecture.Features*` lines are already global usings in
  `web-spa/global-usings.cs`: 11 unconditional ones, plus `Superheros` (`#if(grpc)`) and `WeatherForecasts`. So the
  file's `#if grpc` / `#if api` using blocks repeat the global ones. Only `Admin.Ai`, `Admin.Feedback`,
  `Admin.Principals`, `Admin.Roles` and `DesignSystem` are new. The build does not enforce IDE0005, so nothing fails.
- Suggestion: keep only the non-global usings. The two template-conditional using blocks then go away, and only the
  `#if` around the two `[JsonSerializable]` lines stays.
- Status: open
