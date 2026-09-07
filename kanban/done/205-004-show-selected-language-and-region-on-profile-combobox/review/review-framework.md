# Review framework — task 205-004

**Date:** 2026-09-07
**Host task:** kanban/in-progress/205-004-show-selected-language-and-region-on-profile-combobox/
**Diff scope:** branch `task/205-004-show-selected-language-and-region-on-profile-combo` vs `origin/master` (commits `47254712`, `123db9d4`). Uncommitted `.gitignore` is out of scope (local journal/memsearch ignore, not product).
**Plan / brief:** Closed Language/Region FluentCombobox must show catalog labels (`English (United States)` / `United States`), not Placeholder (`Search languages` / `Search regions`). Search remains; `FreeOption` unset; Theme `FluentSelect` unchanged; no mock auth; `SetIsoCulture` stays `en-US`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle Grok `01a07b57-c908-7731-8abd-42e8083e5e89` (2026-09-07)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Product files in scope

- `source/container-apps/web/projects/web-spa/pages/ProfilePage.razor`
- `source/container-apps/web/features/profile/profile-details-contracts.cs` (`ProfileCatalog.Matching` / `LabelFor`)
- `source/container-apps/web/features/profile/update-profile/update-profile-tests.cs`
- `tools/dev-cli/services/template-smoke-harness.cs` (web-jaribu expected 134 → 135)

## Implementer claim (must re-verify)

Fluent UI Blazor v5 `FluentCombobox` (`FluentSelect.Initialize`) does not write `OptionText` into the closed input from `@bind-Value` when `TOption` (`ProfileCatalog.Entry`) ≠ `TValue` (`string`). Fix: also `@bind-SelectedItems` to the matching catalog row.

Implementer did **not** exercise live `/Profile` closed-field UX.

## Fluent v5 evidence for reviewers

Package: `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.5-26219.1`.

`FluentSelect<TOption,TValue>.OnAfterRenderAsync` (first render):

```csharp
TValue value = base.Value;
string text = ((value is TOption item) ? base.GetOptionText(item) : "");
await JSRuntime.InvokeFluentVoidAsync("Microsoft.FluentUI.Blazor.Components.Select.Initialize", Id, text);
```

`FluentListBase.GetOptionSelected` uses `SelectedItems` only when `Multiple` is true; single-select uses `CurrentValue` + `OptionValue`.

Decompile with:

```bash
DLL="$HOME/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.5-26219.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.dll"
dnx ilspycmd --disable-updatecheck -t 'Microsoft.FluentUI.AspNetCore.Components.FluentSelect`2' "$DLL"
dnx ilspycmd --disable-updatecheck -t 'Microsoft.FluentUI.AspNetCore.Components.FluentListBase`2' "$DLL"
```
