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

## Goal (implementer restatement)

When the maintainer runs `dev run`, the State DevTools inspector must also receive action spans
from the browser (the WASM half of InteractiveAuto), stamped `timewarp.state.runtime=wasm`, next to
the server prerender spans 335-001 already exports. Nothing of it may reach a Release build, a
published WASM bundle, or any publish manifest.

Acceptance:

1. Every `TimeWarp.State*` pin is 12.0.0-beta.15 and `TimeWarp.State.DevTools.Wasm` 12.0.0-beta.15 is
   added; the graph resolves one State version.
2. web-spa `Main` resolves the inspector origin with the package's two-step API (Development only),
   turns on snapshots only when an origin resolves, and registers the browser exporter. The package
   reference is Debug-only and the call sits under `#if DEBUG`.
3. web-server maps `MapTimeWarpStateDevToolsClientConfig()` (the package's Development-only discovery
   route) so the browser learns the origin.
4. The inspector's CORS list covers the origins the browser uses in `dev run`: web-server direct
   (the package adds it) and the YARP ingress (added by the AppHost).
5. Publish suites still prove no DevTools resource or variable leaks; a Release web-spa build has
   no DevTools.Wasm assembly.
6. An automated browser test proves a WASM store action reaches an inspector ingest as a `wasm`
   span. No `dev run`.
7. Design regions and `tw-deploy` say what is exported and what never ships; the
   "WASM not exported" statements are gone. Gates green.

## Checklist

- [x] All `TimeWarp.State*` pins -> 12.0.0-beta.15; DevTools.Wasm added; one resolved version
- [x] web-spa registers the WASM exporter (Development only, endpoint-gated, Debug-only reference, `#if DEBUG`)
- [x] web-server maps `MapTimeWarpStateDevToolsClientConfig()` (Development, gated)
- [x] Inspector CORS covers the dev origins (ingress and direct) or the gap is recorded and fixed
- [x] Publish safety facts hold; no DevTools code in a Release/production WASM bundle (what the package supports recorded)
- [x] Automated check that a browser store appears as `live` (wasm runtime span) in the inspector
- [x] Design regions and `tw-deploy` note updated; flag combinations + `dev template-smoke`
- [x] Gates: `dev check-version` (bump template `<Version>` and platform pins in the same commit if it says so), `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Results

### What changed

- **Pins:** every `TimeWarp.State*` pin is 12.0.0-beta.15 and `TimeWarp.State.DevTools.Wasm`
  12.0.0-beta.15 is added (`Directory.Packages.props`). No package split affects existing references.
  The restore graphs of web-server, the AppHost and the Playwright tests resolve only beta.15
  (State, Blazor, Plus, Telemetry, DevTools.Export, DevTools.Wasm, DevTools.Hosting, DevTools.App).
- **web-spa:** `Program.Main` calls `AddStateDevToolsExportAsync` (two-step form:
  `ResolveEndpointAsync`, then `AddTimeWarpStateTelemetry` with snapshots, then
  `AddTimeWarpStateDevToolsWasmExporter`). It is Development-gated by the package and returns when no
  origin resolves. The PackageReference has `Condition="'$(Configuration)' == 'Debug'"`. The usings,
  call and method compile only with the DEBUG symbol. Each DEBUG block is inside the
  `//-:cnd:noEmit` … `//+:cnd:noEmit` escape. Without it, dotnet new treats `DEBUG` as an undefined
  template symbol and strips the block (TWA0008 caught this). I generated an app and confirmed the
  blocks survive. Snapshots use `DefaultJsonTypeInfoResolver`. IL2026 and IL3050 are suppressed with
  a justification: Debug WASM is not trimmed. `ConfigureServices` is not touched, because
  web-server prerender has its own server exporter.
- **web-server:** `ConfigureEndpoints` calls `MapTimeWarpStateDevToolsClientConfig()`. The package
  maps the route only in Development and returns 204 when `TIMEWARP_STATE_DEVTOOLS_ENDPOINT` is unset.
  The YARP `{**catch-all}` web route reaches it, and there is no fallback auth policy.
- **CORS:** `WithTimeWarpStateDevTools` lists only web-server's own http and https endpoints. In
  `dev run` the browser loads the SPA from the ingress, so the ingress origin was **not** covered. The
  fix is in the AppHost's yarp run-mode block, under the web flag. It sets
  `TimeWarp__State__DevTools__CorsOrigins` (the inspector's supported configuration key, unioned with
  the package variable) on the inspector to the ingress https and http endpoints, plus
  `Ingress:PublicUrl` when set. No upstream change was needed.
- **Publish safety:** the compose, kubernetes and aca `Publish_Should_HaveNoStateDevTools` facts now
  also reject any DevTools CORS variable. A Release `dotnet publish` of web-spa has
  `TimeWarp.State.Telemetry.*.wasm` but no `TimeWarp.State.DevTools.Wasm.*` in `_framework`. A
  string scan of webcil `.wasm` files did not work: the positive controls also failed to match, so I
  am not claiming anything from it. The package has no trimming switch. Its supported shape is
  exactly the Debug-only reference plus a DEBUG-only call, and that is what web-spa now uses.
- **Proof (automated):** new
  `tests/container-apps/web/web-spa-playwright-tests/state-dev-tools-wasm-playwright-tests.cs`. It runs
  an in-process stand-in inspector with the real ingest contract: `POST /ingest/spans` behind CORS for
  the web origin and Content-Type only. It sets `TIMEWARP_STATE_DEVTOOLS_ENDPOINT`, hosts web-server
  in-proc (Development, InteractiveWebAssembly) and drives headless Chromium. No AppHost is started.
  Both configurations assert that the discovery route returns the origin.
  - **Debug** (web-spa's `AssemblyConfiguration` is Debug): a span must arrive with
    `timewarp.state.runtime=wasm`, `timewarp.state.render=interactive` and a `timewarp.state.action`.
    Observed: `ApplicationState.Initialized` with a `state.snapshot` event.
  - **Release:** the browser must not request `/_timewarp/state-devtools` or any `DevTools.Wasm`
    asset, and the inspector must receive nothing.
  - **Limitation:** `dev test` and CI build Release, so the gate runs only the Release branch. The live
    Debug branch runs with `dotnet run -c Debug` (see How to validate). This is a consequence of the
    Debug-only package requirement.
- **AppHost model:** new fact `RunMode_Should_AllowIngressOriginsOnInspector`. With yarp on, the
  inspector's CORS variable is built from the ingress https and http endpoints. With yarp off the
  variable is absent.
- **Docs:** Design regions in the AppHost, web-server and web-spa, the web-spa.csproj comments and
  the `tw-deploy` "never ships" row are updated. The 335-001 "WASM not exported" statements are gone.
- A Debug start in Development with no inspector costs one discovery GET, which returns 204.

### Gates

- `dev check-version`: source 2.0.0-beta.20 is ahead of NuGet 2.0.0-beta.19, so no bump is needed.
- `dev build` (Release): 0 warnings, 0 errors. Debug build of the web-spa → web-server →
  Playwright-tests graph: 0 warnings, 0 errors.
- `dev test --group fast`: 14/14 projects passed. `dev test --group playwright`: passed, including the
  new test's Release branch.
- `dev test --group integration` did not finish inside the 10-minute foreground limit (3-way
  parallel), so I ran each integration project alone with `dotnet test -c Release --no-build`:
  aspire-tests 63/63, yarp-integration 17/17, web-infrastructure 78/78, web-jaribu 359/359,
  api-jaribu 9/9, api-server-integration 1/1, web-spa-integration 289/289, web-server-integration
  346 passed, 1 skipped, 0 failed.
- Debug browser proof (`dotnet run -c Debug -- --filter-class StateDevToolsWasm`): 1/1.
- `dev template-smoke`: SUCCEEDED. `dev template-smoke --matrix`: SUCCEEDED (Default, NoPostgres,
  NoApi, NoWeb, NoGrpc, NoYarp).
- `ganda repo audit`: passes all checks.

### How to validate

Smoke:

```bash
cd tests/container-apps/web/web-spa-playwright-tests
dotnet run -c Debug -- --filter-class StateDevToolsWasm
dotnet run -c Release -- --filter-class StateDevToolsWasm
cd ../../aspire/aspire-tests && dotnet run -c Release -- --filter-class StateDevToolsModel
```

Expect:

- Debug: `succeeded: 1`, plus a `[335-002] wasm span from the browser:` line whose attributes include
  `"timewarp.state.runtime":"wasm"` and `"timewarp.state.render":"interactive"`.
- Release: `succeeded: 1`. No discovery request is made and no span is posted.
- StateDevToolsModel: `succeeded: 3`.
- Maintainer, after merge: in `dev run`, open the dashboard link **TimeWarp.State DevTools**, then load
  the app through the ingress. The inspector shows `live` rows with the `wasm` badge next to the `pre`
  (server) rows.

### Review disposition

- Rounds: 1. Roster: general (review oracle, claude-opus-5-5). Effort 2 (by-diff, 497 lines).
- Final counts: bug 0, suggestion 1 wontfix, nit 0. Open: 0.
- Disposition: **accepted-exceptions**. M1 (suggestion): no gate runs the Debug live wasm-span
  branch, because `dev test` and CI build Release. It is accepted because the Debug-only reference
  is a requirement and the scripted Debug run is recorded. Adding a Debug lane is a lane-policy
  call for the maintainer (follow-up).
- The reviewer re-ran the Debug browser proof (1/1, an `ApplicationState.Initialized` span with
  `runtime=wasm`) and StateDevToolsModel (3/3). The implementer restatement matches the task, with
  no misread.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.

## Notes

- Workers must not start the maintainer's AppHost (`dev run`); the maintainer opens the inspector after merge.
- No time estimates; scope, dependencies and proof gates only.

## Session

- Created: 2026-10-16 (cockpit follow-up to 335-001; unblocked by timewarp-state 121 / v12.0.0-beta.15)
- 2026-10-16 implement (ganda task work): wiring, ingress CORS, Playwright proof (Debug live / Release absent), publish facts, docs; gates above.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-16T14:18:55Z
