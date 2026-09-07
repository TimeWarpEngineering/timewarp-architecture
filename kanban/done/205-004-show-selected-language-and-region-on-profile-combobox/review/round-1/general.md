# Round 1 — general
**Date:** 2026-09-07
**Scope reviewed:** branch vs origin/master — Profile combobox closed-label display

## Summary

The change adds `ProfileCatalog.Matching` / `LabelFor`, binds Language/Region Comboboxes with `@bind-SelectedItems` to matching catalog rows, adds a host-free catalog lookup test, and bumps web-jaribu smoke expected count 134 → 135. Locked constraints hold (`FreeOption` unset, Theme still `FluentSelect`, no mock auth, `SetIsoCulture` untouched). Decompile of Fluent UI Blazor `5.0.0-rc.5-26219.1` shows the resting-UX fix does not work: first-render `Select.Initialize` ignores `SelectedItems` when `TOption` ≠ `TValue`, so closed fields still get `""` and keep Placeholder.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/pages/ProfilePage.razor:89-110
- Description: `@bind-SelectedItems` does not fix the closed Combobox label. Decompiled `FluentSelect<TOption,TValue>.OnAfterRenderAsync` (first render) is:

  ```csharp
  TValue value = base.Value;
  string text = ((value is TOption item) ? base.GetOptionText(item) : "");
  await JSRuntime.InvokeFluentVoidAsync("…Select.Initialize", Id, text);
  ```

  With `TOption=ProfileCatalog.Entry` and `TValue=string`, `value is TOption` is false, so `text` is always `""`. Package JS `Select.Initialize` then does `_control.value = u` only for `type==="combobox"` — empty string leaves Placeholder (`Search languages` / `Search regions`). `SelectedItems` is never read here. Separately, `FluentListBase.GetOptionSelected` uses `SelectedItems` only when `Multiple` is true; single-select uses `CurrentValue` + `OptionValue`, so the list can show a selected row while the closed input stays on Placeholder. There is no `Select.UpdateValue` / `GetSelectedSingleOption` in this package version — the Design-region claim that SelectedItems feeds Initialize/UpdateValue is false. Theme still “works” because dropdown type does not rely on that combobox `_control.value` write.
- Suggestion: Make Initialize receive real OptionText. Smallest options: (1) `TOption == TValue == string` with `Items` as codes and `OptionText` → `ProfileCatalog.LabelFor(...)`, keep `@bind-Value` on `Details.Language` / `Details.Region`; or (2) bind `Value` as `Entry` (`TOption == TValue == Entry`) and sync `.Code` into the command on change/save. Drop the ineffective `@bind-SelectedItems` unless Multiple is actually needed. Do not dump an unscrolled 800-row `FluentSelect`. Re-verify closed text on load / after Save / on revisit (live `/Profile` or equivalent).
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/features/profile/update-profile/update-profile-tests.cs:229-247
- Description: `MatchingCatalogCode_Should_ReturnCatalogLabel` correctly asserts catalog lookup (`en-US` → `English (United States)`, etc.). That is not proof the closed Fluent input shows that label. Results honestly say live closed-field UX was not exercised and that SPA cannot assert `Select.Initialize` text, and How to validate documents live steps — good. Checking the resting-UX checklist item as done on the strength of SelectedItems + this catalog test is still a false substitute for the product bug, especially given Issue 1.
- Suggestion: Keep the catalog test as lookup coverage. Uncheck or reopen the closed-label checklist until Initialize actually gets OptionText and someone runs the documented `/Profile` steps (or an automated closed-input assertion if that becomes feasible).
- Status: open
