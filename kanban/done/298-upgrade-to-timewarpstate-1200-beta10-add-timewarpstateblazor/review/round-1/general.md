# Round 1 — general
**Date:** 2026-10-10
**Scope reviewed:** branch vs master (17 files, 93+/13-)

## Summary

The change pins TimeWarp.State, TimeWarp.State.Blazor, and TimeWarp.State.Plus at 12.0.0-beta.10.
It references the new Blazor package from web-spa with the same `contentFiles` exclusion. It calls
`AddTimeWarpStateBlazor()` after `AddTimeWarpState()` in web-spa `Program` and in all ten test
hosts that build the SPA mediator. Web.Server now maps Razor components from the
`TimeWarp.State.Blazor` assembly. `BaseCacheableState<TState>` uses the self-referencing
constraint that Plus now requires. Risk is low.

Claims checked against the repo and the restored packages:

- Every `AddTimeWarpState(` call site in `source/` and `tests/` is followed by
  `AddTimeWarpStateBlazor()`. Web.Server registers through `Web.Spa.Program.ConfigureServices`,
  so it is covered.
- The `TimeWarp.State.Blazor` static web asset BasePath is still `_content/TimeWarp.State`
  (`build/TimeWarp.State.Blazor.PackageAssets.json`). The `.ts` / `.d.ts` imports of
  `/_content/TimeWarp.State/js/*` therefore still resolve, and the csproj comment is correct.
- The `TimeWarp.State` 12.0.0-beta.10 package no longer ships `staticwebassets`. Razor
  components and JS moved to the Blazor package, which matches the `AddAdditionalAssemblies`
  change.
- `dev check-version` passes (2.0.0-beta.20 is ahead of nuget 2.0.0-beta.19).
- The Results section correctly explains why the cloner `ICloneable` item does not apply to
  the published beta.10.

## Issues

None.
