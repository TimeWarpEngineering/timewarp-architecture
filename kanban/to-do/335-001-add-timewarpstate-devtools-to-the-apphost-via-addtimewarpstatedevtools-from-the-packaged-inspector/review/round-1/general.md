# Round 1 — general
**Date:** 2026-10-16
**Scope reviewed:** branch vs master, all 14 files (product + tests + tw-deploy + task.md)

## Summary

The change pins every TimeWarp.State package to 12.0.0-beta.14, adds the packaged inspector through
`AddTimeWarpStateDevTools()` / `WithTimeWarpStateDevTools()` inside the AppHost's `IsRunMode` block,
registers the server-side span exporter in web-server (Development, endpoint set), and adds a model
test, three publish safety facts and a closed-box fact. Risk is low.

Checked and not an issue:
- web-spa now weaves `TelemetryBehavior` into every ClientPipeline action, and production/WASM never
  call `AddTimeWarpStateTelemetry`. The generated sender resolves it with `GetRequiredService`, but
  every telemetry dependency of its constructor is an optional parameter
  (`TimeWarpStateTelemetryOptions? = null`, `StateSnapshotCache? = null`, `IStateRenderKind? = null`),
  so DI resolves it. With no listener, the hot path is `HasListeners()` then `next()`.
- No `AddSource("*")` anywhere, so production tracing does not pick up State spans.
- Hosting package reference and global using are web-gated, and the test names are literals, so web-off compiles.

Gates re-run by the reviewer: `StateDevToolsModel_Given_` 2/2; `ComposePublish_Given_` 10/10;
`KubernetesPublish_Given_` 11/11; `AcaPublish_Given_` 11/11; `ganda repo audit` passes.

## Issues

### Issue 1 — Severity: suggestion
- File: kanban/.../task.md (Checklist, item 2)
- Description: The item "SPA endpoint wired end to end" is checked, but the SPA receives nothing and the
  WASM store is not exported (Results, "Decision / limit"). The checklist overstates delivery.
- Suggestion: Reword the item to match what shipped and point at the documented limit.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-server/program.cs:152
- Description: The task asks to wire the SPA "end to end". Only server-side stores (prerender,
  InteractiveServer) reach the inspector. The WASM store, where InteractiveAuto ends up, is not
  inspected. The implementer documented why: the browser has no tracer provider, there is no
  WASM-capable exporter in timewarp-state, and the inspector is cross-origin (CORS).
- Suggestion: Accept it for this id and record WASM inspection as an upstream follow-up
  (timewarp-state WASM exporter), not more code here.
- Status: open

### Issue 3 — Severity: nit
- File: kanban/.../task.md (How to validate)
- Description: The smoke command `dotnet test --project tests/container-apps/aspire/aspire-tests/... -- --filter-class StateDevToolsModel`
  fails from the repo root with `MSB1001: Unknown switch` (the root global.json has no MTP runner, so
  VSTest runs). It works from the test folder (its global.json selects Microsoft.Testing.Platform),
  and the class name is `StateDevToolsModel_Given_`.
- Suggestion: Say to run it from `tests/container-apps/aspire/aspire-tests/` and use the full class name.
- Status: open
