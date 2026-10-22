# Round 2 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/composites/FilterBar.razor:72
- Description: Escape (and the browser's search-clear after it) published `""` twice; Escape on
  an empty box fetched with no filter change.
- Source: general (round 1)
- Disposition notes: a78c089c9 — `LastPublished` baseline from `SearchText`; equal text is
  skipped (immediate and delayed) and the pending wait is cancelled. Verified in round 2.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/components/filter-bar-render-tests.cs:190
- Description: No FilterBar-level test for the immediate clear and Escape paths.
- Source: general (round 1)
- Disposition notes: a78c089c9 — clear, Escape, repeated-text, and return-before-delay cases on
  `ManualDelay`. 14/14 pass.

### M3 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/components/filter-bar-render-tests.cs:161
- Description: Tests reached the private handler through reflection.
- Source: general (round 1)
- Disposition notes: a78c089c9 — handlers `internal`; reflection helper removed.

### M4 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/components/composites/FilterBar.razor:74
- Description: The skip compares raw text while `SetFilter` trims, so a trailing-space change
  after a pause causes one fetch with an unchanged filter.
- Suggestion: leave normalization to the consumer, or skip the fetch in page/state when the
  normalized filter is unchanged.
- Source: general (round 2)
- Disposition notes: wontfix (review oracle, 2026-10-22). FilterBar is brand-neutral and its
  contract is the raw box text; trimming is the consuming state's rule, not the bar's. The cost
  is one redundant fetch per pause with a correct result, outside round-1 M1's scope (Escape /
  clear double fetch). A state-level "skip unchanged filter" belongs with the list-page
  pattern if it is wanted.

## Duplicates / conflicts

- None. Single reviewer.
