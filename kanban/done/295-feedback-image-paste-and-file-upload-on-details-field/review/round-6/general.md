# Round 6 — general
**Date:** 2026-10-10
**Scope reviewed:** 8b29b0356..7d884d8a3: `source/container-apps/web/projects/web-spa/source/features/feedback-paste.ts`, the `feedback-paste-js-module.cs` Design region, and the Client section of `documentation/developer/guides/feedback-attachments.md`.

## Summary

The fix corrects the Fluent UI v5 tag (`fluent-textarea`), accepts the page's wrapper or the control itself, and binds the shadow-root textarea when it appears. Binding uses a MutationObserver on the root and the shadow root, `customElements.whenDefined`, and one animation-frame retry. A composed paste hits the host's capture listener first. That listener calls `stopPropagation`, so the inner listener does not fire and the file is uploaded once. A non-composed paste on the inner textarea, like the Playwright test's, never reaches the host, so the inner listener uploads it. Dispose disconnects the observers, cancels the pending frame, and removes the listeners. `imageFiles` falls back to `items` when `files` is empty. The fix is sound. `web-spa` builds in Release with 0 warnings and 0 errors, and that build compiles the TypeScript.

## Issues

### Issue 1 — Severity: nit
- File: documentation/developer/guides/feedback-attachments.md:109
- Description: The edit left one line of the Client paragraph unwrapped (about 130 characters), while the rest of the guide wraps near 95.
- Suggestion: Reflow the paragraph.
- Status: open
