# Round 1 — merged findings
**Date:** 2026-09-07
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/pages/ProfilePage.razor:89-110
- Description: `@bind-SelectedItems` does not fix the closed Combobox label. Decompiled `FluentSelect<TOption,TValue>.OnAfterRenderAsync` (first render) writes `Select.Initialize` with `Value is TOption ? GetOptionText(Value) : ""`. With `TOption=ProfileCatalog.Entry` and `TValue=string`, that text is always `""`, so the combobox input stays on Placeholder (`Search languages` / `Search regions`). `SelectedItems` is not read on that path. Single-select `GetOptionSelected` also ignores `SelectedItems` (uses `CurrentValue` + `OptionValue`), which is why the open list can show the right row while the closed field does not. There is no `Select.UpdateValue` / `GetSelectedSingleOption` in `5.0.0-rc.5-26219.1`. Theme still works because dropdown type does not use that combobox `_control.value` write.
- Suggestion: Make Initialize receive real OptionText. Smallest options: (1) `TOption == TValue == string` with `Items` as codes and `OptionText` → `ProfileCatalog.LabelFor(...)`, keep `@bind-Value` on `Details.Language` / `Details.Region`; or (2) bind `Value` as `Entry` (`TOption == TValue == Entry`) and sync `.Code` into the command. Drop ineffective `@bind-SelectedItems` unless Multiple is needed. Do not dump an unscrolled 800-row `FluentSelect`. Re-verify closed text on load / after Save / on revisit.
- Source: general
- Disposition notes: Fixed on this task. Language/Region Combobox is `TOption == TValue == string` with `Items` = LanguageCodes/RegionCodes and OptionText → LabelFor. SelectedItems binding removed. Initialize now gets GetOptionText(ISO tag).

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/profile/update-profile/update-profile-tests.cs:229-247
- Description: `MatchingCatalogCode_Should_ReturnCatalogLabel` asserts catalog lookup, not that the closed Fluent input shows that label. Results honestly say live closed-field UX was not exercised. Checking the resting-UX checklist item as done on SelectedItems + this catalog test is a false substitute for the product bug (M1).
- Suggestion: Keep the catalog test as lookup coverage. Do not treat the closed-label checklist as done until Initialize actually gets OptionText. Document live `/Profile` steps (already present) and re-run them after the M1 fix if a browser is available.
- Source: general
- Disposition notes: Addressed with M1. Catalog test kept as lookup coverage (LanguageCodes/RegionCodes + LabelFor). Closed-input proof is the Fluent Initialize path (TOption == TValue) plus documented live `/Profile` steps. SPA host still cannot assert Select.Initialize JS text.

## Duplicates / conflicts

- None. M2 is coverage/process around the same root defect as M1; keep both IDs.
