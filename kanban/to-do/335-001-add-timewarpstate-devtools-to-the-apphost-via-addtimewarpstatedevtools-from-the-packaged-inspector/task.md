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

- [ ] State pins → 12.0.0-beta.14; DevTools.Hosting added; single version resolved
- [ ] `AddTimeWarpStateDevTools()` + `WithTimeWarpStateDevTools()` in run mode; SPA endpoint wired end to end
- [ ] Not in any publish output (+ safety facts)
- [ ] Flag combinations + template-smoke
- [ ] Tests; Design region + skill note
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Notes

- Workers must not start the maintainer's AppHost (`dev run`). The maintainer checks the DevTools
  dashboard resource after merge.

## Session

- Created: 2026-10-16 (cockpit; unblocked by timewarp-state 118 / v12.0.0-beta.14)
