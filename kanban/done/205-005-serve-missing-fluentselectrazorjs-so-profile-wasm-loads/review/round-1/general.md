# Round 1 — general
**Date:** 2026-09-08
**Scope reviewed:** branch vs origin/master — FluentSelect.razor.js MapStaticAssets overlay

## Summary

The change overlays one JS module onto the exact `_content/…/Components/List/FluentSelect.razor.js` path WASM was reported to `import()`, without `UseStaticFiles` or a Fluent UI downgrade. Overlay body matches rc.5 `FluentSelect.ts` / `lib.module.js` (`Select.Initialize` writes combobox `_control` text + a11y; `ClearValue`); `export var Microsoft` plus `globalThis` publish. MSBuild `AddFluentSelectJsOverlayStaticWebAsset` (Discovered, `BasePath=/`) is in web-server Release SWA endpoints as that unfingerprinted route with `text/javascript`. Re-ran `dotnet test -c Release -- --filter-class FluentSelectJsOverlay`: 3/3 passed. Profile still uses Fluent combobox/select (205-004 labels, 205-002 catalogs).

rc.5 C# does **not** `import()` that collocated file (Fluent UI #5074; `InvokeFluentVoidAsync` on globals that `lib.module.js` already publishes). The overlay still meets the written product requirement (GET 200 at that URL with those identifiers) and is a valid stopgap for the observed 404. Comment inaccuracy is not a product defect. Live Aspire remains master; in-proc GET is the automated proof.

## Issues

<!-- none -->
