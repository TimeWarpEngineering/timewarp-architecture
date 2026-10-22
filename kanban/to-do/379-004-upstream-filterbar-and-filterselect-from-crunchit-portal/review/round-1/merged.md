# Round 1 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/components/composites/FilterBar.razor:44
- Description: Escape publishes `""` immediately, even when `""` is already the published
  text. On a `type=search` input, the browser's Escape clear also raises input `""`, so one
  Escape runs `SetFilter` + `FetchFeedbackList` twice. Escape on an empty box, or text that
  returns to the last published value within the delay, also fetches with no filter change.
- Suggestion: track the last published text. Skip a publish (immediate or delayed) equal to it,
  while still cancelling a pending wait. Note the rule in Design.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/components/filter-bar-render-tests.cs:118
- Description: "A clear fires immediately" is tested only on `SearchDebouncer`, with
  `immediate: true` passed by hand. FilterBar's empty-text → immediate rule
  (`FilterBar.razor:41`) and its Escape keydown (`FilterBar.razor:44`) have no test.
- Suggestion: add FilterBar-level cases on `ManualDelay`, with no release: `"acme"` then `""` →
  `[""]`, and `"acme"` then Escape → `[""]`. Add a repeated-text case for M1.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/components/filter-bar-render-tests.cs:201
- Description: The tests call the private `OnSearchChanged` through reflection, so a rename
  fails only at runtime. `InternalsVisibleTo web-spa-integration-tests` already exists.
- Suggestion: make the handlers `internal`, call them directly, and drop the reflection helper.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer.
