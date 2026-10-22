# Round 1 — general
**Date:** 2026-10-22
**Scope reviewed:** same as framework (`5742efb7c..a4d5ff241`, product paths only)

## Summary

The change does what the spec decided. There is no PageHeader component. `TimeWarpPage` has
optional `Description` and `Actions` slots, and when both are unset the markup is the same as
before. `SectionHeader` is a native-root composite with `AdditionalAttributes`. CSS is
isolated, uses `--twe-*` tokens, and has no `<style>` element or `::deep`. Purpose and Design
regions are present. Risk is low.

I re-ran these gates:

- `./bin/dev build`: 0 warnings, 0 errors.
- `ganda repo audit`: passes.
- `dotnet test -- --filter-class SectionHeader_Should_`: 4/4.
- `dotnet test -- --filter-class TimeWarpPage_Should_`: 4/4.
- The product diff has no "crunchit" match.

I did not re-run `./bin/dev template-smoke`.

There is one analyzer suppression in the test harness, and that code has a fix that needs no
suppression. The rest are maintainability and polish findings.

## Issues

### Issue 1 — Severity: bug
- File: tests/container-apps/web/web-spa-integration-tests/components/time-warp-page-render-tests.cs:240
- Description: `ShellHost.BuildRenderTree` wraps `child.AddAttribute(sequence, …)` in
  `#pragma warning disable ASP0006` / `restore`. ASP0006 is the analyzer for non-literal
  `RenderTreeBuilder` sequence numbers. The pragma only silences that analyzer so the
  warnings-as-errors build passes. The comment says "the sequence cannot be a literal", which
  is false: `RenderTreeBuilder.AddMultipleAttributes(int sequence, IEnumerable<KeyValuePair<string, object?>>)`
  takes one literal sequence for a whole parameter dictionary, and it works for component
  frames. That is how Razor compiles `@attributes` on a component. Under the review policy, a
  suppression that only makes an analyzer pass is severity bug.
- Suggestion: Replace the loop and the pragma with `child.AddMultipleAttributes(1, PageParameters);`.
  Cast or retype the dictionary if the nullable annotation needs it. Delete the `#pragma` pair
  and the comment. Another fix: have the test pass a `RenderFragment` that the caller builds
  with literal sequences.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor:129
- Description: The title block (the `TwPageTitle`/`PageTitle` branch on `ShowInBreadcrumbs`,
  plus `<h1 class="twe-page__title">`) is copied into both arms of
  `@if (Description is null && Actions is null)`. The two copies must stay identical, or the
  shell's single-h1 and breadcrumb behaviour will differ depending on whether a page sets a
  slot. AGENTS.md hard rule: "When two things must agree, generate one from the other."
- Suggestion: Write the title markup once, for example as a private
  `RenderFragment TitleBlock => @<text>…</text>;` (or a `RenderFragment` field) in `@code`.
  Render `@TitleBlock` in both arms, so the only branch is the wrapping header div.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/to-do/pages/TodoItemsPage.razor:10
- Description: "Items on this list." is a placeholder. It says nothing the "Todo Items" title
  does not already say. The page is the template's reference adopter of the description slot,
  so generated apps copy it.
- Suggestion: Write a description that tells the user what they can do on the page, for
  example "Track tasks and mark them done." (match what `TodoItemList` actually offers).
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor:119
- Description: The page body is now inside `<ChildContent>`, but it was not re-indented. Its
  `<div class="twe-stack">` sits at the same column as `<ChildContent>`.
  `StyleGuidePage.razor` and `TodoItemsPage.razor` were indented correctly.
- Suggestion: Indent the wrapped body by one level (2 spaces) to match the other two pages.
- Status: open
