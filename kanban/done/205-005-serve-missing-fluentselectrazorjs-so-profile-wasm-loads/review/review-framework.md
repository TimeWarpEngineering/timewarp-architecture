# Review framework — task 205-005

**Date:** 2026-09-08
**Host task:** kanban/in-progress/205-005-serve-missing-fluentselectrazorjs-so-profile-wasm-loads/
**Diff scope:** branch `task/205-005-serve-missing-fluentselectrazorjs-so-profile-wasm` vs `origin/master` (commits `31b9b5b7`, `ecd5fa42`).
**Plan / brief:** Serve `GET /_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/FluentSelect.razor.js` as a JS module so `/Profile` WASM does not yellow-bar on that URL. Keep Language/Region/Theme as Fluent combobox/select. Do not add `UseStaticFiles`. Do not downgrade Fluent UI. Do not vendor the nupkg.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle Grok `01a07ecb-e410-7190-8016-b49719b72366` (2026-09-08)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Product files in scope

- `source/container-apps/web/projects/web-spa/fluent-ui-overlays/fluent-select.js`
- `source/container-apps/web/projects/web-spa/web-spa.csproj` (`AddFluentSelectJsOverlayStaticWebAsset`)
- `tests/container-apps/web/web-server-integration-tests/features/profile/fluent-select-js-overlay-tests.cs`

## Implementer claim (must re-verify)

Fluent UI `5.0.0-rc.5-26219.1` still `import()`s collocated `Components/List/FluentSelect.razor.js` and calls `Microsoft.FluentUI.Blazor.Components.Select.Initialize` / `ClearValue`, but the nupkg does not pack that file. Overlay maps `fluent-select.js` onto that exact MapStaticAssets `_content` path.

## Fluent v5 evidence for reviewers

Package: `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.5-26219.1`.

`FluentSelect<TOption,TValue>.OnAfterRenderAsync` (first render) does **not** call `FluentJSModule.ImportJavaScriptModuleAsync`. It is:

```csharp
TValue value = base.Value;
string text = ((value is TOption item) ? base.GetOptionText(item) : "");
await JSRuntime.InvokeFluentVoidAsync("Microsoft.FluentUI.Blazor.Components.Select.Initialize", Id, text);
```

`InvokeFluentVoidAsync` is `IJSRuntime.InvokeVoidAsync` on a global identifier (swallows only `JSDisconnectedException` / `OperationCanceledException` / `InvalidOperationException`). The DLL contains **zero** `FluentSelect.razor.js` strings. Fluent UI #5074 moved Select JS into `Core.Scripts`; `lib.module.js` already assigns `window.Microsoft.FluentUI.Blazor.Components.Select` (Initialize + ClearValue). The rc.5 nupkg `staticwebassets/Components/` has no `List/` folder.

Decompile with:

```bash
DLL="$HOME/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.5-26219.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.dll"
dnx ilspycmd --disable-updatecheck -t 'Microsoft.FluentUI.AspNetCore.Components.FluentSelect`2' "$DLL"
dnx ilspycmd --disable-updatecheck -t 'Microsoft.FluentUI.AspNetCore.Components.JSRuntimeExtensions' "$DLL"
```
