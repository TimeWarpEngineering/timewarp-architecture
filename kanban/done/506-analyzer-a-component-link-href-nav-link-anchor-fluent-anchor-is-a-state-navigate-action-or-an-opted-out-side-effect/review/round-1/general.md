# Round 1 — general
**Date:** 2026-11-25
**Scope reviewed:** branch `task/506-analyzer-a-component-link-href-nav-link-anchor` vs `master` (TWA0050, web-spa link conversions, skill, analyzer tests, recorded `## Results`)

## Summary

The restatement matches the original task. TWA0050 is an error diagnostic that reads Razor render-tree calls for `Href` / `href` / `To`, `<a href>`, and static markup anchors, with the external-scheme and `[DirectComponentSideEffect]` exemptions the task asked for. Web-spa hits are RouteLinks, FluentButtons that dispatch `RouteState.ChangeRoute` or `RoleState.OpenNewRole`, or opted-out anchors whose Design regions name the reason. The skill no longer says navigation is `FluentAnchor`, and TWA0050 sits beside TWA0026. Recorded gates cover the analyzer tests, the touched web-spa suites, `dev build` 0/0, `lint-skills`, and `ganda repo audit`. No misread and no product defect.

One targeted check, not a gate: `ilspycmd` on `TimeWarp.State.Plus` 12.0.0-beta.19 `RouteState.ChangeRouteActionSet.Handler` and on `Microsoft.AspNetCore.Components.WebAssembly` 11.0.0 `WebAssemblyNavigationManager`, plus the `navigateTo` function in that package's `blazor.webassembly.js`. The question was whether a RouteLink whose `Route` is an absolute http(s) URL (Ask support, privacy, assembly info) would swallow the click. `ChangeRoute` calls `ToAbsoluteUri` then `NavigateTo`, and the script full-loads when the URI is outside the base. That matches the RouteLink Design note.

## Issues

<!-- None. -->
