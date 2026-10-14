# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** commit 21ff7e050 plus call sites (AgentAsk composer, AskStarterPrompts, AskAnswerBar)

## Summary

The change keeps the library `MessageInput` send button as the send action and overlays a
click-through FluentUI ArrowUp glyph; CSS hides the paper plane and drives muted
(`:placeholder-shown` / `:disabled`) vs solid primary states. `AskSendButton` swaps in a circular
Stop while the cascaded `AgentContext` streams, calling `CancelAsync`, with its own status
subscription (same pattern as `AskAnswerBar`). Verified: `MessageInput` has a `Placeholder`, so the
`:placeholder-shown` muted rule matches; FluentButton `Tooltip` renders through the tooltip service,
not as an extra grid child; Stop calling the library conversation directly matches the existing
`AskStarterPrompts` precedent for library-owned conversation calls. Tests cover idle/streaming/cancel
markup and the muted→solid→send Playwright path. Risk is low.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskSendButton.razor:60
- Description: Activating Stop from the keyboard removes the focused element (the Stop button
  unmounts when status returns to Idle), so focus falls to `body` instead of the composer.
- Suggestion: After cancel, return focus to the composer textarea or the send button.
- Status: open
