# Export the web-spa WebAssembly stores to State DevTools with TimeWarp.State.DevTools.Wasm (beta.15)

Child of [[335-timewarp-devtools-shell-blazor-shell-hosting-dev-tools-as-plugins]]. Follow-up to
[[335-001-add-timewarpstate-devtools-to-the-apphost-via-addtimewarpstatedevtools-from-the-packaged-inspector]],
which exported server-side stores only and left the WASM store out of scope (its review finding M2).

## Why

The app runs InteractiveAuto. In `dev run` the inspector today shows only `pre` (server prerender)
stores. The browser (WASM) stores are not exported, so `live` rows from the browser never appear
(diagnosed 2026-10-16).

TimeWarp.State **12.0.0-beta.15** is on NuGet (nine packages) and adds the new
`TimeWarp.State.DevTools.Wasm` (timewarp-state task 121,
`/home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-state/master/kanban/done/121-export-blazor-webassembly-store-telemetry-to-the-state-devtools-inspector/task.md`).
That closes the upstream gap 335-001 named.

## Goal

In `dev run`, the State DevTools inspector shows `live` stores from the browser (WASM) next to the
server prerender stores. Nothing about it reaches publish output or a Release/production WASM bundle.

## What the package does (read-only findings, timewarp-state source)

Source: `timewarp-state/master/source/timewarp-state-devtools-wasm/` (`timewarp-state-dev-tools-wasm.cs`,
`dev-tools-wasm-exporter.cs`, `readme.md`).

- **Registration API** (namespace `TimeWarp.State.DevTools.Wasm`, class `TimeWarpStateDevToolsWasm`):
  - `await builder.AddTimeWarpStateDevToolsWasmExporterAsync()` on `WebAssemblyHostBuilder`: in
    Development only, resolves the origin and registers `DevToolsWasmExporter`; a missing origin
    registers nothing (no listener, no request). Outside Development it returns without reading config.
  - Or two-step: `string? endpoint = await TimeWarpStateDevToolsWasm.ResolveEndpointAsync(isDevelopment, configuredEndpoint, baseAddress)`
    then `services.AddTimeWarpStateDevToolsWasmExporter(endpoint)`. Use this form to enable
    `options.IncludeSnapshots = true` (and `JsonSerializerOptions`) in `AddTimeWarpStateTelemetry` only
    when an origin resolves; this is what timewarp-state `samples/04-telemetry/wasm/sample-04-wasm/program.cs` does.
- **Endpoint discovery in the browser** (the supported mechanism), in order: configuration key
  `TimeWarp:State:DevTools:Endpoint`; env var `TIMEWARP_STATE_DEVTOOLS_ENDPOINT` (empty in the browser);
  then `GET {base}/_timewarp/state-devtools` on the SPA's own origin, body `{ "endpoint": "<origin>" }`,
  204 when unset. The host maps that route with `MapTimeWarpStateDevToolsClientConfig()`
  (`TimeWarp.State.DevTools.Export`, `timewarp-state-dev-tools-client-config.cs`), Development only, reading
  `TIMEWARP_STATE_DEVTOOLS_ENDPOINT`. Web-server must therefore call it (it does not today).
- **Transport:** `HttpClient.SendAsync` to `{origin}/ingest/spans` (the same dev-only JSON document the
  server exporter posts, not OTLP); async flush loop, 200 ms coalesce; failures swallowed; spans are
  stamped `timewarp.state.runtime=wasm` (server spans `server`); the inspector log badge shows `srv`/`wasm`.
- **CORS:** the inspector registers a named CORS policy on `POST /ingest/spans` only, Development only,
  limited to origins in `TIMEWARP_STATE_DEVTOOLS_CORS_ORIGINS`. `WithTimeWarpStateDevTools(...)` sets that
  variable on the inspector from each inspected app's http and https endpoints (two calls keep both
  apps' origins). The packaged inspector is started as Development by run mode (`ASPNETCORE_ENVIRONMENT`;
  publish mode does not set it). So the AppHost needs no manual CORS config for web-server's direct
  endpoints; **verify** whether the ingress (YARP) origin the browser actually uses in `dev run` is
  also included, and if not, add it (supplement the variable or an upstream issue). Never backward-pin.
- **Trimming out of Release:** the readme's supported shape is a `Condition="'$(Configuration)' == 'Debug'"`
  PackageReference plus `#if DEBUG` around the call, so a Release publish contains neither the
  assembly nor the call. The package does not reference OpenTelemetry and shares its wire types as
  internal sources. The package has no trimming switch of its own beyond that. Record what the
  implementer confirms.

## Architecture side (current state)

- Pins: `Directory.Packages.props` lines 177-185 hold `TimeWarp.State`, `.Blazor`, `.Plus`, `.Telemetry`,
  `.DevTools.Export`, `.DevTools.Hosting` at **12.0.0-beta.14**. `msbuild/timewarp-platform-packages.props`
  has no State version (references only; check for pin text). `web-spa.csproj` references State,
  State.Blazor, State.Plus and State.Telemetry.
- web-spa: `source/container-apps/web/projects/web-spa/program.cs` (`Main` -> `ConfigureServices`;
  `builder.HostEnvironment.IsDevelopment()` available; Design region mentions beta.13, refresh it).
- web-server: `source/container-apps/web/projects/web-server/program.cs` `AddStateDevToolsExport`
  (lines ~159-185), gated on Development and `TIMEWARP_STATE_DEVTOOLS_ENDPOINT`. No
  `MapTimeWarpStateDevToolsClientConfig()` call exists.
- AppHost: `source/container-apps/aspire/projects/aspire-app-host/program.cs` ~359-362,
  `AddTimeWarpStateDevTools()` + `webServer.WithTimeWarpStateDevTools(stateDevTools)` inside the
  `IsRunMode` block in `#if web`.
- Existing tests: `tests/container-apps/aspire/aspire-tests/state-dev-tools-model-tests.cs`,
  `Publish_Should_HaveNoStateDevTools` in `compose-publish-tests.cs`, `kubernetes-publish-tests.cs`,
  `aca-publish-tests.cs`, closed-box `ingress-smoke-tests.cs`.

## Requirements

- **Packages:** bump ALL `TimeWarp.State*` pins to **12.0.0-beta.15** (State, Blazor, Plus, Telemetry,
  DevTools.Export, DevTools.Hosting) and add `TimeWarp.State.DevTools.Wasm` 12.0.0-beta.15. Never
  backward-pin. Check the other new beta.15 packages for splits that affect existing references. Verify
  one resolved State version across the graph.
- **web-spa wiring:** Development-only and only when the inspector endpoint resolves; use the two-step
  form so snapshots turn on only then. Add the PackageReference Debug-only and compile the call out of
  Release (`#if DEBUG`), keeping `#if web` / `#if api` / `#if grpc` regions intact. Add a TWA0004
  Purpose region to any new file.
- **Endpoint reaches the browser** via the package's discovery route: web-server calls
  `MapTimeWarpStateDevToolsClientConfig()` (Development, same gate as the exporter). Do not invent a
  different mechanism.
- **CORS:** confirm the origins the browser app uses in dev (ingress and direct web-server) are allowed
  by the inspector; add what is missing if the package supports it.
- **No effect on publish:** extend `Publish_Should_HaveNoStateDevTools` (compose, kubernetes, aca) if
  anything new could leak (for example the CORS env var); no DevTools code or JS in a Release WASM
  bundle (assert with a build/publish check of web-spa output if cheap; record what the package supports).
- **Proof that a browser store shows as `live`:** an automated check, not manual. Prefer a Playwright or
  aspire closed-box test (existing ingress-smoke lane) asserting the inspector received spans with
  `timewarp.state.runtime=wasm` / render=interactive from the WASM client. If the inspector exposes no
  readable log API, assert through whatever the inspector offers and record it. **Workers must NOT run
  `dev run`** (worktree AppHosts share user secrets); use the test harness lanes only.
- **Docs:** update the AppHost / web-server / web-spa Design regions and the `tw-deploy` note (what
  never ships); remove the "WASM not exported" statements left by 335-001.
- No new template flags (flags are the architecture axis only).

## Checklist

- [ ] All `TimeWarp.State*` pins -> 12.0.0-beta.15; DevTools.Wasm added; one resolved version
- [ ] web-spa registers the WASM exporter (Development only, endpoint-gated, Debug-only reference, `#if DEBUG`)
- [ ] web-server maps `MapTimeWarpStateDevToolsClientConfig()` (Development, gated)
- [ ] Inspector CORS covers the dev origins (ingress and direct) or the gap is recorded and fixed
- [ ] Publish safety facts hold; no DevTools code in a Release/production WASM bundle (what the package supports recorded)
- [ ] Automated check that a browser store appears as `live` (wasm runtime span) in the inspector
- [ ] Design regions and `tw-deploy` note updated; flag combinations + `dev template-smoke`
- [ ] Gates: `dev check-version` (bump template `<Version>` and platform pins in the same commit if it says so), `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Notes

- Workers must not start the maintainer's AppHost (`dev run`); the maintainer opens the inspector after merge.
- No time estimates; scope, dependencies and proof gates only.

## Session

- Created: 2026-10-16 (cockpit follow-up to 335-001; unblocked by timewarp-state 121 / v12.0.0-beta.15)
