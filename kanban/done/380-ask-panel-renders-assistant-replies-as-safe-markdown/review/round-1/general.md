# Round 1 — general
**Date:** 2026-10-17
**Scope reviewed:** commit 891008202 (AgentAsk.razor/.css, safe-markdown.cs, xai-chat-upstream-server.cs,
safe-markdown-render-tests.cs, ask-markdown-playwright-tests.cs)

## Summary

The change adds a `BlockRenderer<RichContentBlock>` in `AgentAsk.razor` that routes assistant
text through `SafeMarkdown` and keeps user turns as plain text (told apart by `ContentBlock.Role`).
`SafeMarkdown` gains three opt-in parameters (`OnAppLinkClick`, `ExternalLinksInNewTab`,
`SoftBreaksAsSpaces`), so feedback output does not change. It also fixes a null `Lines` crash on an
open fence. The safety model is unchanged: links still pass `SafeMarkdownPolicy.IsSafeLink`, which
refuses `//host`, so the `url[0] == '/'` app-link branch cannot be reached by a
protocol-relative URL. Ask passes no image allow-list. Render-tree sequence numbers (58, 83–85) are
unique. The `#pragma warning disable BL0006` in the test double is scoped, has a reason, and matches
the precedent in `notification-copy-tests.cs`. It does not silence a correctness guard.

Gates re-run by the reviewer: `ganda repo audit` passes; `dev build` 0 warnings / 0 errors;
`dev test` 23/23 projects passed (including web-spa-integration and web-spa-playwright).

Goal restatement vs task: consistent. No misread.

## Issues

### Issue 1 — Severity: nit
- File: tests/container-apps/web/web-spa-playwright-tests/ask-markdown-playwright-tests.cs:100
- Description: The browser test proves "no reload", but Blazor's own `<a href>` interception
  would also give no reload. So the browser test alone cannot show that the click went through
  `RouteState.ChangeRoute` and not the default link handling. The unit test
  `Send_App_Link_Clicks_To_The_Callback` does prove the preventDefault plus callback wiring.
- Suggestion: Optionally assert that the anchor carries the Blazor preventDefault, or add a
  state-observable marker. The current combined coverage is enough.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/composites/safe-markdown.cs:256
- Description: With `OnAppLinkClick` set, the `onclick` preventDefault is unconditional.
  Ctrl/Cmd+click on an in-app link therefore routes in place and does not open a new tab. Middle
  click (`auxclick`) still works.
- Suggestion: Accept for now. Blazor cannot make preventDefault conditional per event without JS.
- Status: open
