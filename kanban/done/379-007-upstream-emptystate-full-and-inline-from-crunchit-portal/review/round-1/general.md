# Round 1 — general
**Date:** 2026-10-23
**Scope reviewed:** commit 05385f757 vs master (`source/`, `tests/`), plus task.md Goal restatement and Results.

## Summary

The change adds one native `EmptyState` (`components/elements/EmptyState.razor`, `.razor.css`,
`empty-state-variant.cs`) with a Full and an Inline variant. It adopts the inline form on the admin
feedback grid and on "Your filings", adds a style-guide card and an overview row, and adds seven
HtmlRenderer tests. It follows the 379-00x conventions: `twe-` native root, `Class` plus
`CaptureUnmatchedValues`, isolated CSS on `--twe-*` tokens, Purpose and Design, no `<style>`, no
`DisplayComponent`, and no events wired in the component. The implementation matches both the task
and the implementer's restatement. Risk is low. One Design claim about screen-reader behaviour is
inaccurate. There are two small polish items.

Gates were re-run in this worktree:

- `./bin/dev build`: 0 warnings, 0 errors.
- `dotnet test -c Release --no-build -- --filter-class EmptyState_Should_`: 7/7. The binary is newer than the sources.
- `ganda repo audit`: passes.
- `./bin/dev template-smoke`: SUCCEEDED.
- The client-name grep over `git diff master -- source tests` matches nothing.

The `AdminFeedbackEmpty` selector is not used by any test outside `artifacts/`, so adding the
description text to that element breaks no assertion.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/elements/EmptyState.razor:11
- Description: The Design region states as fact: "it announces when the region is inserted or its
  text changes. A list that becomes empty after a filter is announced." Screen readers announce
  reliably only when the *content* of a live region that is *already in the DOM* changes. When an
  element is inserted with `role="status"` and its text in the same render, NVDA and JAWS often
  stay silent, and VoiceOver varies. Both adopters insert the region fresh. The grid inserts
  `EmptyContent` when the row count drops to zero, and "Your filings" swaps the branch in after
  `Section`'s loading spinner. So the task's goal, "an empty list that appears after a filter
  changes should be announced", is not reliably met, while the Design text says it is. Design
  regions are the record, so a wrong a11y claim there gets copied.
- Suggestion: Keep `role="status"`, since it is harmless and helps where it works, but correct the
  Design text. Say that insertion is announced inconsistently across screen readers, and that a page
  needing a guaranteed announcement keeps its own persistent live region (for example a result count
  that is always rendered). Drop the claim that a filter-to-empty transition is announced.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor:160
- Description: The two Full samples sit inside `<Card Title="Empty States">`. The Card renders its
  title as `<h2>` (`Card.razor:39`), and the samples use the default `HeadingLevel` 2. The style guide
  is the reference consumers copy, and here it shows the outline mistake that `HeadingLevel` exists to
  prevent: sibling `h2`s nested under an `h2`.
- Suggestion: Pass `HeadingLevel="3"` on the two Full samples.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor:214
- Description: The adopted titles end in a period ("No feedback matches this filter.", and "No
  feedback filed yet." at `FeedbackListPage.razor:436`). The style-guide titles and the task's own
  example (`Title="No feedback matches this filter"`) have none. A title followed by a sentence
  description reads better without the period, and that matches the samples.
- Suggestion: Drop the trailing period from both adopter titles.
- Status: open
