# Round 1 — general
**Date:** 2026-10-16
**Scope reviewed:** branch vs master (a07c76aec): Directory.Packages.props, web-spa program.cs + csproj,
web-server program.cs, AppHost program.cs + constants.cs, compose/kubernetes/aca publish facts,
state-dev-tools-model-tests.cs, new state-dev-tools-wasm-playwright-tests.cs, tw-deploy SKILL.md.

## Summary

The change bumps every TimeWarp.State pin to 12.0.0-beta.15, adds a Debug-only
TimeWarp.State.DevTools.Wasm reference, and wires the package's two-step browser exporter in
web-spa `Main` under `#if DEBUG` (inside the `cnd:noEmit` escape). It maps the package discovery
route in web-server and adds the ingress origins to the inspector CORS list in run mode. It matches
the task and the implementer restatement (no misread). Low risk: everything is gated by Debug,
Development, run mode and an endpoint that resolves.

Verified against upstream source (timewarp-state master):
- `TimeWarp:State:DevTools:CorsOrigins` is a real inspector key. `ParseOrigins` unions it with
  `TIMEWARP_STATE_DEVTOOLS_CORS_ORIGINS`, splits on `,`/`;`, and normalizes each entry to
  `GetLeftPart(Authority)`. So a comma list of endpoint URLs plus an `Ingress:PublicUrl` with a path
  is parsed correctly.
- `AddTimeWarpStateTelemetry` uses TryAdd, and web-spa registered no telemetry options before, so
  there is no double registration. web-server prerender keeps its own call.
- The YARP Development `{**catch-all}` web route forwards `/_timewarp/state-devtools` to web-server.
- The reflection snapshot resolver plus IL2026/IL3050 suppression is Debug-only. The upstream sample
  uses a source-generated context, but this template's store types are app-owned and open-ended. A
  hand-maintained context would be an agreement-by-memory. The suppression justification is accurate
  (Debug WASM is neither trimmed nor AOT-compiled in `dev run`). Not raised.

Gate claims re-run by the reviewer: `dotnet run -c Debug -- --filter-class StateDevToolsWasm` 1/1, with a
`timewarp.state.runtime=wasm` span `ApplicationState.Initialized`. `aspire-tests --filter-class
StateDevToolsModel` (Release) 3/3.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-spa-playwright-tests/state-dev-tools-wasm-playwright-tests.cs:482
- Description: The positive proof (a wasm span reaches the inspector) runs only when web-spa is built
  Debug. `dev test` and CI build Release, so no gate runs the live branch. Only the Release negative
  branch is gated. A regression in the Debug wiring (for example a discovery-route or CORS break) would
  not fail any PR.
- Suggestion: Add a Debug lane for this one test class to `dev test` (for example a playwright-debug
  group) or to the nightly workflow. Otherwise, accept the gap with the Debug command recorded in
  How to validate.
- Status: open
