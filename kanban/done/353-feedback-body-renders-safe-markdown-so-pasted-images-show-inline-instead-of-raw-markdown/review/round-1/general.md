# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** commit d492f2844 (SafeMarkdownPolicy, SafeMarkdown, FeedbackBody, MarkdownImage, both pages, unit + Playwright tests)

## Summary

The change renders feedback bodies by walking the Markdig AST into the render tree, with no
MarkupString, so text and attributes are always escaped by Blazor. The link allow-list
(http(s), `/path` not `//`, `#frag`, no whitespace/control/backslash) and the exact-match image
allow-list meet the requirements, and the XSS tests cover the listed vectors. No security bugs
were found. Three small consistency points remain.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/composites/safe-markdown-policy.cs:62
- Description: `ReferencedSources` counts every `LinkInline` descendant, including a link nested in an image description (`![see [log](/api/…/id)](/x)`). SafeMarkdown flattens image descriptions to alt text, so that inner link never renders, yet the attachment is left out of the list. It then appears nowhere on the page.
- Suggestion: skip links whose ancestor is an image `LinkInline`, so the dedupe matches what renders. Add a test.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/composites/safe-markdown.cs:7
- Description: The Design region says an `HtmlBlock` that still appears "renders as text". `HtmlBlock` is a `LeafBlock` with no inline, so it falls to `default` and renders nothing. (With `DisableHtml()` it never parses anyway.)
- Suggestion: correct the comment.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/composites/FeedbackBody.razor:9
- Description: A Design comment line runs well past the surrounding wrap width.
- Suggestion: reflow.
- Status: open
