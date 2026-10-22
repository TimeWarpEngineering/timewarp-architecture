# Round 2 — general
**Date:** 2026-10-22
**Scope reviewed:** fix delta `7b6c6e683..a78c089c9` (round-1 base → fix commit), product paths
`FilterBar.razor`, `filter-bar-search-debounce.cs`, `filter-bar-render-tests.cs`; re-verify
round-1 M1–M3.

## Summary

The fix pass adds a `LastPublished` baseline to `FilterBar` (seeded from `SearchText` on the
first parameter set), skips any immediate or delayed publish equal to it, and cancels the
pending wait on that path through a new `SearchDebouncer.Cancel`. `PublishAsync` re-checks the
baseline, so a delayed publish that matches is also dropped. The handlers are now `internal`
and the tests call them directly; four new FilterBar-level cases on the manual clock cover
clear, Escape, repeated text, and a return to the baseline before the delay. Low risk. Only
`FeedbackAdminListPage` sets `FeedbackAdminState.Search`, and only through this bar, so seeding
the baseline once is sound for the adopting page.

Verification: `dotnet run -- --filter-class FilterBarRender_` in
`tests/container-apps/web/web-spa-integration-tests` built (warnings are errors) and passed
14/14.

## Prior findings

- **M1 (suggestion)** — fixed. Escape followed by the browser's search-clear publishes once;
  Escape on an empty box publishes nothing (`Repeated_Text_Does_Not_Publish_Again`,
  `Text_That_Returns_To_The_Published_Value_Before_The_Delay_Publishes_Nothing`). Design
  region records the rule in both files.
- **M2 (suggestion)** — fixed. `A_Clear_Of_Published_Text_Fires_Immediately_On_The_Bar` and
  `Escape_On_Published_Text_Fires_Immediately_On_The_Bar` run on `ManualDelay` with no release,
  so they prove the immediate path on the component.
- **M3 (nit)** — fixed. `OnSearchChanged` / `OnSearchKeyDown` are `internal`; the reflection
  helper and `using System.Reflection` are gone.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/composites/FilterBar.razor:74
- Description: The skip compares raw box text, but `SetFilter` trims and maps whitespace to
  null (`feedback-admin-state.set-filter.cs:52`). Typing `acme`, pausing, then adding a
  trailing space and pausing publishes `"acme "`, which stores the same `Search` and fetches
  again. Same for `"  "` then `""`. One extra fetch per pause; no wrong result.
- Suggestion: either leave normalization to the consumer (FilterBar's contract is the raw box
  text), or have the page/state skip a fetch when the normalized filter is unchanged.
- Status: open
