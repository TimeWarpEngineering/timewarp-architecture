# Round 1 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/components/avatar-render-tests.cs:40
- Description: `Leave_The_Default_Size_Without_A_Modifier` passes `Size = Medium` explicitly, so
  no test checks that the default size has no modifier class, which the checklist requires.
  Changing the `Size` default in `Avatar.razor` would break no test.
- Suggestion: Render without `Size` in that test, or assert `ShouldNotContain("twe-avatar--")`
  on the root in `Render_The_Initials_On_A_Twe_Avatar_Root`.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/components/elements/avatar-initials.cs:26
- Description: The `words.Length == 0` check (line 26) and the `LengthInTextElements == 0` check
  in `FirstTextElement` (line 44) can never fire once the null-or-whitespace guard has passed.
  This dead code ships in every generated app.
- Suggestion: Remove both guards (`StringInfo.GetNextTextElement(word)` is enough).
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. There was one reviewer.

## Misread check

- The implementer restatement matches the task's Description and Checklist. No misread.
