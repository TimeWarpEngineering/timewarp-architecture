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

- [ ] Confirm current pins and all TimeWarp.State consumers
- [ ] Read beta.10 release notes; list every breaking change that applies here
- [ ] Bump TimeWarp.State and TimeWarp.State.Plus to 12.0.0-beta.10
- [ ] Add TimeWarp.State.Blazor 12.0.0-beta.10 and reference it where needed
- [ ] Fix Blazor-split compile/registration changes
- [ ] Remove/replace FeatureFlagState usage (task 100)
- [ ] Add ICloneable where the source-generated cloner cannot clone a type (task 097)
- [ ] Full build green
- [ ] Unit, end-to-end and browser (Playwright/WASM) tests green in CI

## Acceptance

- Full ganda walk via `ganda task work 298 --yes` (implement, review, audit, done-move, PR).
- CI green, including end-to-end and browser tests.
- Merge only via `ganda pr merge` after Steven approves.
- Do not run the app on TWE-001; browser proof comes from CI.

## Notes

- Filed from a voice call with Steven (via Amina), 2026-10-10. Left in to-do, unclaimed, not launched.

## Session

- Created: 2026-10-10
