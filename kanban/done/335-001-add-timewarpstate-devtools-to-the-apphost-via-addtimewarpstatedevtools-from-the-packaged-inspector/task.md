# Add TimeWarp.State DevTools to the AppHost via `AddTimeWarpStateDevTools()` from the packaged inspector

Child of [[335-timewarp-devtools-shell-blazor-shell-hosting-dev-tools-as-plugins]] (step 2 of its build
plan, done the clean way).

## Why

TimeWarp.State **12.0.0-beta.14** (2026-10-16; timewarp-state task 118) ships the DevTools inspector as
a packaged app, following the Aspire dashboard pattern:
- `TimeWarp.State.DevTools.Hosting` 12.0.0-beta.14 depends on `TimeWarp.State.DevTools.App`
  12.0.0-beta.14 (the published inspector).
- `AddTimeWarpStateDevTools()` runs it as an executable resource, with no source path, folder walk or
  env var.

Architecture's AppHost can now use it with one call. Every generated app then gets State DevTools in
`dev run`.

## Goal (implementer restatement)

Every generated app's `dev run` starts the packaged TimeWarp.State DevTools inspector next to
web-server and web-server sends its State action spans there, while nothing about it reaches
Compose, Kubernetes or ACA publish output.

Acceptance:
- All `TimeWarp.State*` packages resolve to 12.0.0-beta.14 (one version); the AppHost references
  `TimeWarp.State.DevTools.Hosting`.
- Run mode: a `timewarp-state-devtools` resource exists; web-server carries
  `TIMEWARP_STATE_DEVTOOLS_ENDPOINT` (its http endpoint) and waits for it; web-server registers the
  State ActivitySource + DevTools exporter in Development only.
- Publish mode: no resource, no variable; a fact in each of the compose, kubernetes and aca suites.
- `web` off: no DevTools wiring and still compiles (template-smoke).
- Model test, closed-box "it runs and serves" fact, AppHost Design region, `tw-deploy` note.
- `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit` green.

## Requirements

- **Packages:** move every `TimeWarp.State*` pin (State, Blazor, Plus, and any others in use) from
  12.0.0-beta.13 to **12.0.0-beta.14**, and add `TimeWarp.State.DevTools.Hosting` 12.0.0-beta.14 to the
  AppHost. Verify the resolved graph has a single State version.
- **AppHost wiring (`aspire-app-host/program.cs`):**
  - `AddTimeWarpStateDevTools()`;
  - `WithTimeWarpStateDevTools(devTools)` on the resource(s) running TimeWarp.State, which is
    web-server (WASM is served by web-server; check how the SPA receives
    `TIMEWARP_STATE_DEVTOOLS_ENDPOINT`, for example via web-server config to the client, and wire it
    end to end);
  - follow the Hosting package's readme and the State demo AppHost as the reference usage.
- **Run mode only.** DevTools never appears in Compose, Kubernetes or ACA publish output. Gate on
  `ExecutionContext.IsRunMode`, like the other dev-only resources (REPL, browser-log forwarding), and
  add a fact to each publish safety suite (compose, kubernetes, aca) asserting no DevTools resource
  or `TIMEWARP_STATE_DEVTOOLS_ENDPOINT`.
- **SPA side:** if the State client needs registration to talk to DevTools (an export or telemetry
  service from `TimeWarp.State.DevTools.Export`), add it in Development only and keep
  production unchanged. Keep timewarp-state task 103's separate-store rule.
- **Template-flag aware:** works with every flag combination (`web` off means no DevTools wiring).
  `dev template-smoke` stays green.
- **Tests:**
  - an aspire model test: run mode has the DevTools resource, and web-server carries the endpoint
    reference;
  - the publish safety facts above;
  - a closed-box check (helper from 319) that the DevTools resource starts and is healthy, if cheap.
- **Docs:** the AppHost Design region describes DevTools (what it is, run-mode only, where it comes
  from). Add a short note in the relevant skill (`tw-deploy` "what never ships", and/or the
  dev-loop docs).

## Checklist

- [x] State pins → 12.0.0-beta.14; DevTools.Hosting added; single version resolved
- [x] `AddTimeWarpStateDevTools()` + `WithTimeWarpStateDevTools()` in run mode; server-side stores exported end to end (WASM store out of scope, see Results)
- [x] Not in any publish output (+ safety facts)
- [x] Flag combinations + template-smoke
- [x] Tests; Design region + skill note
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Results

- **Packages:** `TimeWarp.State`, `.Blazor`, `.Plus` → 12.0.0-beta.14; added `TimeWarp.State.Telemetry`,
  `TimeWarp.State.DevTools.Export`, `TimeWarp.State.DevTools.Hosting` at 12.0.0-beta.14. Resolved
  graphs (aspire-app-host, web-server, web-spa `project.assets.json`) contain only beta.14 State packages.
  The App package stamps `timewarpstatedevtoolspath` onto the AppHost assembly (checked in AssemblyInfo).
- **AppHost:** `AddTimeWarpStateDevTools()` + `webServer.WithTimeWarpStateDevTools(...)` inside the
  existing `IsRunMode` block in the `#if web` section; Hosting package reference and global using are
  web-gated. The Design region has the full explanation.
- **End-to-end path:** web-spa references `TimeWarp.State.Telemetry`, so `TelemetryBehavior` is
  woven into its `ClientPipeline`. With no listener (WASM, production) it emits no spans. web-server
  (`AddStateDevToolsExport`) registers `AddTimeWarpStateTelemetry` (snapshots on, reflection JSON
  resolver) and the `TimeWarp.State` source + `AddTimeWarpStateDevToolsExporter()`, only in
  **Development and only when `TIMEWARP_STATE_DEVTOOLS_ENDPOINT` is set**. That covers the server-side
  stores (prerender + InteractiveServer, i.e. the server half of InteractiveAuto).
- **Decision / limit:** the WASM store is **not** exported. The browser has no tracer provider, and
  the inspector is a different origin (it would need CORS and a WASM exporter). The variable stays on
  web-server, and the SPA receives nothing. If WASM-side inspection is wanted, that is a follow-up
  that needs an upstream WASM-capable exporter in timewarp-state.
- **Tests:** `state-dev-tools-model-tests.cs` (run mode: the resource has an `http` endpoint,
  web-server's `TIMEWARP_STATE_DEVTOOLS_ENDPOINT` is that EndpointReference, and web-server waits on it.
  Publish model: no resource and no wait). `Publish_Should_HaveNoStateDevTools` is in the compose,
  kubernetes and aca suites. `IngressSmoke_Given_.StateDevTools_Should_RunAndServeItsUi` is the closed-box check
  that the inspector reaches Running and serves `/` with 200.
- **Docs:** AppHost Design region, web-server Design region, publish-suite Design regions, and the
  `tw-deploy` "Never ships" table row plus the dev-convenient rule list.
- **Gates:** `dev build` 0 warnings/0 errors; `dev test` 23/23 projects passed (aspire-tests 57/57);
  `dev template-smoke` SUCCEEDED (Default, NoPostgres, NoApi); `ganda repo audit` passes;
  `dev check-version` shows 2.0.0-beta.20 is already ahead of NuGet beta.19, so no bump.
- **web off:** template-smoke has no web-off variant. I generated `--web false` by hand. The AppHost
  builds with 0 errors and has no DevTools code. That solution's test projects already fail
  (`ingress-smoke-tests` uses `WebServerApiRoutePrefixes`, and timewarp-testing has IDE0005/RCS1213).
  Those failures are not from this change and are not fixed here.

- **Review:** 1 round, effort 2, roster `general`. Final counts: bug 0; suggestion 1 fixed + 1 wontfix;
  nit 1 fixed; 0 open. Disposition **accepted-exceptions**. M2 (WASM store not inspected) is accepted
  because it needs an upstream WASM-capable exporter. The reviewer re-ran the model tests (2/2), compose
  (10/10), kubernetes (11/11) and aca (11/11) suites and `ganda repo audit` (pass). Paths:
  `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke, from `tests/container-apps/aspire/aspire-tests/` (its global.json selects Microsoft.Testing.Platform; from the repo root
`dotnet test` falls back to VSTest and rejects `--project`): `dotnet test --project aspire-tests.csproj -- --filter-class StateDevToolsModel_Given_`,
then the full `aspire-tests` (publish suites + ingress smoke). After merge, the maintainer runs `dev run` and opens the
dashboard, then clicks a few SPA actions while the page is still server-interactive.

Expect: 2/2 model facts pass and aspire-tests 57/57. In `dev run` the dashboard lists a
`timewarp-state-devtools` resource with a **TimeWarp.State DevTools** link, and web-server shows
`TIMEWARP_STATE_DEVTOOLS_ENDPOINT` in its environment. The inspector shows `pre`/`live` rows for
server-side stores. `dev publish compose|kubernetes|aca` output has no DevTools resource or variable.

## Review

- Round 1: Done

## Notes

- Follow-up (not filed): WASM-side State DevTools inspection needs a WASM-capable exporter plus
  inspector CORS in timewarp-state (review M2). The maintainer decides whether to file it there.

- Workers must not start the maintainer's AppHost (`dev run`). The maintainer checks the DevTools
  dashboard resource after merge.

## Learnings

- Misread (restatement vs task): the restatement narrowed "check how the SPA receives
  `TIMEWARP_STATE_DEVTOOLS_ENDPOINT` ... and wire it end to end" to "web-server sends its State action
  spans there". The narrowing was a sound, documented decision (no WASM exporter upstream). But the
  checklist still claimed "SPA endpoint wired end to end". When a restatement drops part of the task's
  scope, say so in the restatement itself and word the checklist to match.

## Session

- Created: 2026-10-16 (cockpit; unblocked by timewarp-state 118 / v12.0.0-beta.14)
- Boyscout: removed the stale `kanban/to-do/372-*` copy (372 is done; its to-do copy survived the feature/ci-speed merge and tripped `kanban-duplicate-ids`).
- 2026-10-16 implement (Claude Opus 5.5, ganda task work): wired DevTools in run mode with the server-side exporter; all gates green; WASM export is out of scope (see Results).
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-16T03:21:45Z
- 2026-10-16 review oracle (Claude Opus 5.5, ganda task work): tw-implementation-review round 1, general, accepted-exceptions (0 open).
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-16T03:25:00Z
