# Round 1 — general
**Date:** 2026-10-22
**Scope reviewed:** `master...HEAD` (10c9358c6), all 16 product files under `source/` and `tests/`

## Summary

The change adds `FilterBar` (paper toolbar, `role="toolbar"`, `FluentTextInput` search, with a
debounce run by `SearchDebouncer` on an injected `TimeProvider`) and `FilterSelect` (muted label
plus `FluentSelect<string,string>`, `data-qa` forwarded to the select). It also moves the admin
feedback list onto the bar, using the existing `ListFeedback.Query.Search` title filter through
`SetFilter`, then `FetchFeedbackList`. Risk is low. The components hold no filter state, page
handlers only dispatch actions, the CSS uses only tokens and no hex, and `<style>` and Crunchit
names are absent. Both themes below are in the clear and Escape paths: duplicate publishes, and
no test at the component seam.

Re-verified in this worktree: `./bin/dev build --quiet` succeeded. In
`web-spa-integration-tests`, `--filter-class Filter` gave 14/14 and `--filter-class FeedbackAdmin`
gave 11/11. `git diff master...HEAD -- source tests | grep -ic crunchit` printed 0.
`TimeProvider.System` is registered in `Web.Spa.Program.ConfigureServices`, which web-server
prerender composes, so the server path resolves the clock too. `ControlStyle` is FluentUI 5.0.1's
`IFluentControlStyle` shadow-DOM injection (package XML docs), not a `<style>` element. The CA2025
pragmas in the tests wrap the exact dispose-while-pending scenario the tests prove. They do not
silence a missing behavior.

Goal restatement vs task: consistent. No misread.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/composites/FilterBar.razor:44
- Description: Escape always publishes `""` immediately, even when that text was already
  published. With `TextInputType.Search`, the browser's own Escape clear also raises an input
  event with `""`, so `OnSearchChanged("")` publishes too. The admin page then runs
  `SetFilter` + `FetchFeedbackList` twice for one Escape. Escape on an already-empty box also
  fetches with no filter change. The same holds for any text that returns to the last published
  value within the delay (type `bugx`, backspace to `bug`): it publishes `bug` again and fetches
  again.
- Suggestion: remember the last published text in `FilterBar` (or `SearchDebouncer`), and skip a
  publish, immediate or delayed, whose text equals it. A pending wait should still be cancelled.
  Record the rule in the Design region.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/components/filter-bar-render-tests.cs:118
- Description: Checklist item "A clear fires immediately" is proven only at the `SearchDebouncer`
  seam. The test passes `immediate: true` by hand. The `FilterBar` rules that make a clear
  immediate have no test: empty text → `immediate` (`FilterBar.razor:41`), and the Escape keydown
  (`FilterBar.razor:44`). If either regressed, every test would still pass.
- Suggestion: add component-level cases on the `ManualDelay` clock, with no release before the
  assertion. Notify `"acme"` then `""` and assert `[""]`. An Escape `KeyboardEventArgs` after
  `"acme"` also asserts `[""]`. If Issue 1 lands, add a case showing that a repeated text publishes
  once.
- Status: open

### Issue 3 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/components/filter-bar-render-tests.cs:201
- Description: The tests reach the private `OnSearchChanged` through reflection
  (`GetMethod("OnSearchChanged", NonPublic)`). A rename shows up as a runtime null, not a compile
  error. `web-spa.csproj` already has `InternalsVisibleTo web-spa-integration-tests`, which is
  how the tests already reach the internal `SearchDebouncer`.
- Suggestion: make the search and keydown handlers `internal` and call them directly. Drop the
  `System.Reflection` helper.
- Status: open
