# Round 2 — merged findings
**Date:** 2026-09-07
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

Final open count: 0. Prior M1/M2 carried forward as fixed. No new IDs.

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/pages/ProfilePage.razor:85-108
- Description: `@bind-SelectedItems` did not feed `Select.Initialize` when `TOption` ≠ `TValue`.
- Suggestion: `TOption == TValue == string` with OptionText → LabelFor.
- Source: general (round 1); re-verified round 2
- Disposition notes: Language/Region Comboboxes bind string codes; Initialize gets LabelFor text; SelectedItems removed; Theme FluentSelect unchanged.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/profile/update-profile/update-profile-tests.cs:229-252
- Description: Catalog LabelFor test is not closed-input proof.
- Suggestion: Keep as lookup coverage; do not treat resting UX as done until Initialize gets OptionText.
- Source: general (round 1); re-verified round 2
- Disposition notes: Catalog test kept (LanguageCodes/RegionCodes + LabelFor). Closed-input proof is equal type args on Initialize, not the catalog assertion.

## Duplicates / conflicts

- None. No new findings in round 2.
