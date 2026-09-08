# Round 1 — general
**Date:** 2026-09-08
**Scope reviewed:** branch vs origin/master — product commit `b719a8fc` (fluent-select.js, web-spa.csproj overlay comment, fluent-select-js-overlay-tests.cs)

## Summary

The 205-005 overlay imported as 200 but only published `Microsoft.FluentUI.Blazor.Components.Select`. This change adds the live WASM / rc.4 tree `Microsoft.FluentUI.Blazor.Select` (`SetComboBoxValue` + `ClearValue`) while keeping rc.5 `Components.Select` (`Initialize` + `ClearValue`), with `export var Microsoft` and `globalThis` publish of both. Overlay `SetComboBoxValue` / rc.4 `ClearValue` match the rc.4 nupkg collocated JS body; decompiled rc.5 `FluentSelect` uses `InvokeFluentVoidAsync("Microsoft.FluentUI.Blazor.Components.Select.Initialize")` and does not call `SetComboBoxValue`. Node `import()` reports all four identifiers as `'function'`; `FluentSelectJsOverlay` tests 3/3 passed; source and GET body assertions would fail without `Microsoft.FluentUI.Blazor.Select` / `SetComboBoxValue`. No `UseStaticFiles`, Fluent UI pin stays `5.0.0-rc.5-26219.1`, Profile Language/Region `FluentCombobox` and Theme `FluentSelect` unchanged. Csproj still says rc.5 C# `import()`s the collocated file (inaccurate vs decompile; same non-defect bar as 205-005). Live Aspire remains master per Results — disclosed, not a silent miss.

## Issues

<!-- none -->
