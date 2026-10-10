# Task 298: Upgrade to TimeWarp.State 12.0.0-beta.10 (add TimeWarp.State.Blazor)

## Description

Upgrade timewarp-architecture from TimeWarp.State 12.0.0-beta.9 to 12.0.0-beta.10. In beta.10 the
Blazor-specific State code was split out of `TimeWarp.State` into a new `TimeWarp.State.Blazor`
package (timewarp-state PR 625, task 037), and reflection-based cloning was replaced by a
source-generated cloner (task 097, PR 626). Architecture does not reference `TimeWarp.State.Blazor`
anywhere yet, so the upgrade will not compile/behave correctly without adding it.

Current pins on master (verified 2026-10-10, `Directory.Packages.props`):

- line 170: `<PackageVersion Include="TimeWarp.State" Version="12.0.0-beta.9" />`
- line 171: `<PackageVersion Include="TimeWarp.State.Plus" Version="12.0.0-beta.9" />`

Projects referencing TimeWarp.State packages today (csproj grep):

- `source/container-apps/web/projects/web-spa/web-spa.csproj`

Release notes (breaking changes):
https://github.com/TimeWarpEngineering/timewarp-state/blob/master/documentation/release-notes/release12.0.0-beta.10.md
(`documentation/release-notes/release12.0.0-beta.10.md` in timewarp-state)

## Requirements

1. In `Directory.Packages.props` bump `TimeWarp.State` and `TimeWarp.State.Plus` from
   `12.0.0-beta.9` to `12.0.0-beta.10` (confirm the pins above are still current before editing).
2. Add `TimeWarp.State.Blazor` `12.0.0-beta.10` (published on NuGet) to `Directory.Packages.props`
   and reference it from every project that uses the Blazor-specific State code split out in
   beta.10 (start with `web-spa.csproj`; check any other project/test project that uses State's
   Blazor components, base classes, or registration).
3. Handle all beta.10 breaking changes listed in the release notes, including:
   - the Blazor split (namespaces, `using`s, service registration moved to TimeWarp.State.Blazor);
   - the task 100 fixes, including removal of `FeatureFlagState` (remove/replace any usage);
   - the source-generated cloner (task 097, PR 626) that removed reflection cloning: any State
     type the generator cannot clone must implement `ICloneable`.
4. Run the full build and the full CI suite, including end-to-end and browser
   (Playwright / WASM) tests, and fix any failures as part of this task.

## Checklist

- [x] Confirm current pins and all TimeWarp.State consumers
- [x] Read beta.10 release notes; list every breaking change that applies here
- [x] Bump TimeWarp.State and TimeWarp.State.Plus to 12.0.0-beta.10
- [x] Add TimeWarp.State.Blazor 12.0.0-beta.10 and reference it where needed
- [x] Fix Blazor-split compile/registration changes
- [x] Remove/replace FeatureFlagState usage (task 100)
- [x] Add ICloneable where the source-generated cloner cannot clone a type (task 097)
- [x] Full build green
- [x] Unit, end-to-end and browser (Playwright/WASM) tests green locally (`./bin/dev test`; CI re-runs on the PR)

## Results

Pins on this branch before the edit were still `TimeWarp.State` and `TimeWarp.State.Plus`
`12.0.0-beta.9` in `Directory.Packages.props`. The only direct package references were
`source/container-apps/web/projects/web-spa/web-spa.csproj`. Test projects reach those
assemblies through the web-spa project reference.

Published NuGet `12.0.0-beta.10` (tag `v12.0.0-beta.10`, timewarp-state PR 625) breaking
changes that apply here:

- Blazor components, JavaScript interop, Redux DevTools, render subscriptions, and wwwroot
  moved to `TimeWarp.State.Blazor`. Namespaces are unchanged. Blazor hosts call
  `AddTimeWarpStateBlazor()` after `AddTimeWarpState()`. `UseReduxDevTools` and
  `AddJavaScriptDispatch` stay in the `TimeWarp.State` namespace and live in the Blazor
  assembly. `RenderSubscriptionsPostProcessor` requires `RenderSubscriptionContext`, so
  every host that uses `AddWebSpaGeneratedMediator` registers Blazor services.
- `TimeWarpCacheableState<TState>` now requires `where TState : TimeWarpCacheableState<TState>`.
  `BaseCacheableState<TState>` uses `where TState : BaseCacheableState<TState>`.
  `AuthorizationState` already passes itself. Existing `Hydrate` overrides already return
  the concrete state.
- `FeatureFlagState` and `UseFeatureFlags` are gone. This repo has no references to either.
- `InvalidCloneException` now takes a cause. This repo does not construct it.

`web-spa` references `TimeWarp.State.Blazor` `12.0.0-beta.10` with `ExcludeAssets="contentFiles"`
so the package `tsconfig.json` is not compiled by TypeScript.MSBuild. Static web assets stay
under `/_content/TimeWarp.State/`. `Web.Server` maps Razor components from
`TimeWarp.State.Blazor` (where `ReduxDevTools.razor` now lives) and `TimeWarp.State.Plus`.

The source-generated cloner (timewarp-state task 097, PR 626) is not in the published
`12.0.0-beta.10` package. `TimeWarp.State.dll` still contains `DeepCloner` and
`CloneExtensions`. The packed source generator has no `StateCloneSourceGenerator` and no
`TWSG002`. No state here implements `ICloneable` for that generator. That change is
documented as `12.0.0-beta.11`, which is not on NuGet.

`./bin/dev build` succeeded with 0 warnings and 0 errors. `./bin/dev test` succeeded for
every project under `tests/`, including `web-server-integration-tests` (304 passed, 1
skipped), `web-spa-integration-tests` (179 passed), and `web-spa-playwright-tests` (5
passed).

### How to validate

Smoke:

```bash
./bin/dev build
./bin/dev test
```

Expect:

- `./bin/dev build` prints `0 Warning(s)`, `0 Error(s)`, and `Build completed successfully!`.
- `./bin/dev test` prints `Tests completed successfully!`.
- `web-server-integration-tests` reports 304 succeeded and 1 skipped.
- `web-spa-integration-tests` reports 179 succeeded.
- `web-spa-playwright-tests` reports 5 succeeded.

### Review disposition

- Rounds: 1. Roster: general (effort 1).
- Final counts: bug 0, suggestion 0, nit 0. Open 0, fixed 0, wontfix 0.
- Disposition: **clean**.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Acceptance

- Full ganda walk via `ganda task work 298 --yes` (implement, review, audit, done-move, PR).
- CI green, including end-to-end and browser tests.
- Merge only via `ganda pr merge` after Steven approves.
- Do not run the app on TWE-001; browser proof comes from CI.

## Notes

- Filed from a voice call with Steven (via Amina), 2026-10-10. Left in to-do, unclaimed, not launched.
- 2026-10-10: CI `template-smoke` failed after the PR opened (PR #457, run 38037662735, job 114171407302). The `ci` job passed, including Playwright. SmokeDefault built with 0 warnings and 0 errors, then the initializer import-graph check failed: `/_content/TimeWarp.State/js/logger.js` and `/_content/TimeWarp.State/js/constants.js` were looked up under `~/.nuget/packages/timewarp.state/12.0.0-beta.10/staticwebassets/js/`. In beta.10 those files live in the `timewarp.state.blazor` package's `staticwebassets/js/` (same `/_content/TimeWarp.State/` URL path). Fix in scope: `tools/dev-cli/services/template-smoke-initializer-assets.cs` must resolve `/_content/TimeWarp.State/` paths against the `TimeWarp.State.Blazor` package too. Steven approved another walk pass (`ganda task work 298 --restart --no-merge --yes`) to fix it on this branch and PR. First walk log: `~/logs/task-work-timewarp-architecture-298-20261010-150003.log`.

## Session

- Created: 2026-10-10
- Implementation: 2026-10-10 (Grok implement oracle)
- Review: 2026-10-10 (Claude review oracle, effort 1, general) — clean
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 80 — 2026-10-10T08:21:15Z
