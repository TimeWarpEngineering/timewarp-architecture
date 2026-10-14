# Round 1 — merged findings
**Date:** 2026-10-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/composites/safe-markdown-policy.cs:62
- Description: `ReferencedSources` counted links nested in an image description. Those never render, so the attachment vanished from the page.
- Suggestion: skip links inside an image `LinkInline`; add a test.
- Source: general
- Disposition notes: Added `IsInsideImage`; new assert in `Policy_Reports_Only_Given_Sources`. SafeMarkdown suite 9/9 green.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/composites/safe-markdown.cs:7
- Description: Design region claimed an HtmlBlock renders as text; it renders nothing.
- Suggestion: correct the comment.
- Source: general
- Disposition notes: Comment corrected.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/composites/FeedbackBody.razor:9
- Description: Over-long Design comment line.
- Suggestion: reflow.
- Source: general
- Disposition notes: Reflowed.

## Duplicates / conflicts

- None (single reviewer).
