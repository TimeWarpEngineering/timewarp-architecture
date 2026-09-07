# Round 2 — general
**Date:** 2026-09-07
**Scope reviewed:** post-M1 fix delta + prior M1/M2

## Summary

Post-fix Language/Region Comboboxes use `TOption == TValue == string` with `Items` = `LanguageCodes`/`RegionCodes`, `OptionText` → `LabelFor`, and no `SelectedItems` binding. That matches Fluent UI Blazor `5.0.0-rc.5-26219.1` `FluentSelect.OnAfterRenderAsync`: `Value is TOption` now succeeds, so `Select.Initialize` receives catalog labels and the combobox JS path sets `_control.value`. Locked constraints still hold (`FreeOption` unset, Theme remains `FluentSelect` on Entry rows, no mock auth, `SetIsoCulture` en-US, Combobox kept with `Height="20rem"`). No new defects found in the fix delta.

## Resolved prior

- M1: fixed — `ProfilePage.razor` Comboboxes are `TOption="string" TValue="string"`; Initialize text is `GetOptionText(ISO tag)` via `LabelFor` (e.g. `en-US` → `English (United States)`). Without `OptionValue`, `GetOptionValue` identity-casts the code string (`IsOptionTypeCompatibleWithValue`), so `@bind-Value` on `Details.Language`/`Details.Region` still stores ISO tags. `filterOptions` matches `option.text` prefix (LabelFor), so typing `Thai` still finds `Thai (Thailand)`.
- M2: fixed — catalog test kept as lookup coverage (`LanguageCodes`/`RegionCodes` + `LabelFor`/`Matching`). Closed-input proof is the Fluent Initialize path under equal type args, not the catalog assertion; SPA host still cannot assert Initialize JS text.

## Issues

<!-- none -->
