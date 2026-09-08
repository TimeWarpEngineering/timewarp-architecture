# Review framework — task 205-006

**Date:** 2026-09-08
**Host task:** kanban/in-progress/205-006-publish-fluentselect-setcomboboxvalue-on-blazorselect/
**Diff scope:** branch `task/205-006-publish-fluentselect-setcomboboxvalue-on-blazorsel` vs `origin/master` (product commit `b719a8fc`; results `929d9881`).
**Plan / brief:** After 205-005, `GET FluentSelect.razor.js` is 200 but `/Profile` WASM still yellow-bars `Could not find 'Microsoft.FluentUI.Blazor.Select.SetComboBoxValue' ('Select' was undefined)`. Publish that identifier (rc.4 tree) on the overlay without dropping rc.5 `Components.Select.Initialize` / `ClearValue`. Do not revert dropdowns, add `UseStaticFiles`, or bump Fluent UI.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle Grok `01a07f10-dcd3-7671-bbaa-ed7e32c1ca3a` (2026-09-08)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Product files in scope

- `source/container-apps/web/projects/web-spa/fluent-ui-overlays/fluent-select.js`
- `source/container-apps/web/projects/web-spa/web-spa.csproj` (`AddFluentSelectJsOverlayStaticWebAsset` comment)
- `tests/container-apps/web/web-server-integration-tests/features/profile/fluent-select-js-overlay-tests.cs`

## Implementer claim (must re-verify)

The 205-005 overlay imported as 200 but only published `Microsoft.FluentUI.Blazor.Components.Select` (`Initialize` / `ClearValue`). Live WASM JSInterop looks up `Microsoft.FluentUI.Blazor.Select.SetComboBoxValue` on the imported module (`JSObjectReferenceExtensions.InvokeVoidAsync`). Overlay now publishes **both** identifier trees. `SetComboBoxValue(id, value)` is the rc.4 body (`FLUENT-DROPDOWN` `_control.value`). Tests assert the live identifier, not only `Components.Select`.

## Fluent v5 / rc.4 evidence for reviewers

Package pin: `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.5-26219.1`.

**rc.4** `FluentSelect.OnAfterRenderAsync` (matches the live stack):

```csharp
await base.JSModule.ImportJavaScriptModuleAsync("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/FluentSelect.razor.js");
if (string.Equals(DropdownType, "combobox", StringComparison.Ordinal))
{
  await base.JSModule.ObjectReference.InvokeVoidAsync("Microsoft.FluentUI.Blazor.Select.SetComboBoxValue", Id, text);
}
```

rc.4 collocated JS (`FluentSelect.razor.js`) exports only `Microsoft.FluentUI.Blazor.Select` (`ClearValue` + `SetComboBoxValue` writing `_control.value`).

**rc.5** `FluentSelect.OnAfterRenderAsync` does **not** import that file and does **not** call `SetComboBoxValue`:

```csharp
await JSRuntime.InvokeFluentVoidAsync("Microsoft.FluentUI.Blazor.Components.Select.Initialize", Id, text);
```

`lib.module.js` already assigns `window.Microsoft.FluentUI.Blazor.Components.Select` (Initialize + ClearValue). The live yellow-bar identifier is the **rc.4** module path. 205-005 already noted that rc.5 C# does not `import()` the collocated file; the csproj comment still says it does.

Decompile with:

```bash
RC4="$HOME/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.4-26180.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.dll"
RC5="$HOME/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.5-26219.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.dll"
dnx ilspycmd --disable-updatecheck -t 'Microsoft.FluentUI.AspNetCore.Components.FluentSelect`2' "$RC4"
dnx ilspycmd --disable-updatecheck -t 'Microsoft.FluentUI.AspNetCore.Components.FluentSelect`2' "$RC5"
```
