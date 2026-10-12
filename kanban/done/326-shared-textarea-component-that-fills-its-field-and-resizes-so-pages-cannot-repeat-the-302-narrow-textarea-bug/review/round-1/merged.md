# Round 1 — merged findings
**Date:** 2026-10-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/direct-fluent-text-area-analyzer.cs (IsHostPath)
- Description: The host exemption matched on file name only, so any `TextArea.razor` in any folder was exempt.
- Suggestion: Require the `components/forms` segment; test same-named files elsewhere and backslash paths.
- Source: general
- Disposition notes: The match is now by project-relative path. It accepts the Razor generator hint path `components/forms/TextArea_razor.g.cs` (confirmed by emitting generated files), `components_forms_TextArea_razor.g.cs`, or a `#line` path ending in `components/forms/TextArea.razor`, with either separator. Four new tests: same-named tree elsewhere flags, generator hint path for the host is exempt, generator hint path elsewhere flags, backslash `#line` path is exempt. The first attempt only matched the underscore form, and `dev build` flagged TextArea.razor itself; that is how the real hint path was found.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-playwright-tests.cs (SubmitFeedbackAsync)
- Description: Re-clicked Submit up to 5 times, which could file duplicates and hide a submit that did nothing.
- Suggestion: Wait on a deterministic condition, then click once.
- Source: general
- Disposition notes: Now waits until the button has stayed enabled for 500ms (polled every 50ms), clicks once, and fails with a screenshot if no receipt shows within 30s. FeedbackAttachment passed 3/3 on two consecutive runs.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/forms/FormField.razor.css:31
- Description: The `> fluent-textarea` / `> fluent-text-area` direct-child selectors no longer match anything, because the textarea now sits inside `.twe-text-area`. The FormField Design comment still listed them.
- Suggestion: Remove them.
- Source: general
- Disposition notes: Removed both selectors. The Design comment now points multi-line text at TextArea.razor.

## Duplicates / conflicts

- None.
