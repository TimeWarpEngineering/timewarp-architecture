# Round 1 — general
**Date:** 2026-10-13
**Scope reviewed:** commit 7605d2da9

## Summary

The feature is well structured. Every interaction is a TimeWarp.State action, JS goes only through the `[SideEffectService]` `AskTagJsModule` from the StartPick and CancelPick handlers, and the browser's description is re-cleaned and re-authorized in C#. `AskTagChatClient` does not mutate the caller's messages and does not double-append on tool round trips, because it sits outside `FunctionInvokingChatClient`. The main gaps are these:
- The chips are marked sent before the request succeeds.
- The `page_context.tags` copy embedded in the instructions goes stale after the first send.
- Pick mode is not torn down on navigation or a principal change.
- The keyboard capture blocks Enter and Tab on the panel's own Cancel button.
- The `[TrackAction]` on StartPick turns the app-wide busy signal on for the whole pick.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/agent/ask-tag-chat-client.cs:89
- Description: `AttachAsync` awaits `MarkSent()` before `base.GetResponseAsync` or `GetStreamingResponseAsync` runs. If the request then fails (network error, upstream 4xx/5xx, cancellation), the chips have already left the composer and are listed in `Sent` / `page_context.tags`, but the model never received them. A retry or resend of the same text goes out without the tags, because `Pending()` is now empty. The person loses the chips with no indication. A smaller related point: MarkSent moves *all* current `Attached` into `Sent`, not the snapshot that `Pending()` returned, so a chip added between the snapshot and the dispatch is recorded as sent without being sent.
- Suggestion: Mark sent only after the inner call succeeds. For streaming, that means after the first update arrives or the stream completes. On exception, leave the chips attached. Pass the snapshot's ids to MarkSent so that only those chips move.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:242
- Description: The instructions embed `PageAgentContext.Describe(...)`, including the new `tags`, once per agent build. The agent is rebuilt only when the path, edit mode or conversation generation changes. After turn 1's MarkSent, the turn-2 instructions still carry `"tags":[]`. The thread keeps only the typed text (by design), so on later turns the model has no tag in history and an empty `page_context.tags` in its prompt. The tag is recoverable only if the model chooses to call the page_context tool. That contradicts the preface ("page_context.tags lists the ones sent earlier") and the Design note in ask-tag-chat-client.cs ("later turns read the tags from page_context.tags"). The Playwright test passes only because the fake upstream always calls the page_context tool, which re-describes live.
- Suggestion: Rebuild the agent (or refresh `ChatOptions.Instructions`) when `AskTagState.Sent` changes. Alternatively, have `AskTagChatClient` inject the current `SentFor(...)` into the request on every user turn.
- Status: open

### Issue 3 — Severity: bug
- File: source/container-apps/web/projects/web-spa/source/features/ask-tag.ts:215
- Description: `onKeyDown` is a window capture-phase listener that does not exempt the Ask panel (unlike `onPointerDown` and `onClick`, which check `insideAsk`). While picking, Tab never moves focus, so a keyboard user cannot reach the panel's "Cancel" button (`AskTagChips`). Enter pressed on a focused panel button (the Cancel button, or the composer) is swallowed and *chooses* the highlighted page element instead. Esc still works, but the visible Cancel control cannot be used from the keyboard.
- Suggestion: Return early when `insideAsk(event.target)` and the key is Enter or Space, or more broadly let focus inside the panel keep native keys. Alternatively, move focus to a dedicated element during pick mode so the trap is explicit.
- Status: open

### Issue 4 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/ask-tag-state/ask-tag-state.session-changed.cs:418
- Description: `SessionChanged` (and nothing on navigation or AgentAsk dispose) sets `IsPicking = false` and bumps `PickGeneration`, but never calls `AskTagJsModule.CancelPickAsync`. On a principal change during pick mode, the overlay stays up and the capture-phase click, pointer and key listeners stay active. The tracked StartPick action stays in flight, so the panel still shows "Picking" while the state says it is not. Navigation has a related problem. `PickSession` caches `root` (`document.querySelector(rootSelector)`) at construction, and `TimeWarpPage` re-creates `.twe-page__body` per page. After browser Back/Forward during pick mode, `root.contains(...)` is false for every new element: nothing is pickable, any click cancels, and the chip route recorded at StartPick no longer matches the page.
- Suggestion: Dispatch CancelPick from SessionChanged (or call `CancelPickAsync` in its handler) and on `LocationChanged` / AgentAsk dispose. In ask-tag.ts, re-query the root in `candidates()` and `candidateAt()` rather than caching it.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/ask-tag-state/ask-tag-state.start-pick.cs:447
- Description: `[TrackAction]` on StartPick keeps a tracked action active for the entire time the person is picking. `ActionTrackingState.IsActive` is the app-wide busy signal. The footer spinner (`TimeWarpPage.razor:174`) spins throughout pick mode. Pages that use `IsBusy => ActionTrackingState.IsActive` (Settings, Profile, Login, Passkeys, AgentLinks, Authentication) disable their buttons while the person is picking. The page looks busy or disabled exactly while the person is choosing something on it, and the chip text and outline can reflect disabled-state styling.
- Suggestion: Drive the panel's "Picking" UI from `AskTagState.IsPicking`, which already exists, and drop `[TrackAction]`. Alternatively, keep the tracking but exclude this action from the global busy checks.
- Status: open

### Issue 6 — Severity: suggestion
- File: tests/container-apps/web/web-spa-playwright-tests/ask-sign-in-playwright-tests.cs:181
- Description: The spec's Playwright step is "send includes the semantic payload". The test asserts only that the fake upstream's page_context *tool* output contains the tag. That output is live state after MarkSent, so the test does not show that the payload was in the outgoing user message. The integration test covers the message shape with a recording client, but nothing end to end does. The privacy filters in ask-tag.ts (`data-ask-tag-private`, `[hidden]`, `[aria-hidden=true]`, `[inert]`, zero-size and invisible candidates, and hidden descendants excluded from text) have no test at any level. The keyboard path (arrow or Tab, then Enter, to choose) is not exercised either: the test presses Tab then Esc.
- Suggestion: Have the fake upstream echo the last user message's final text part, or assert on the request body it received, and check for `AskTagPayload.MessageHeading` plus the token. Add a Playwright check that a `data-ask-tag-private` or `aria-hidden` marked element is skipped by Tab cycling and hover. Add one Enter-to-choose path.
- Status: open

### Issue 7 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor:138
- Description: The admin grid's mark is on the Title cell's `FluentLink`, but `boxOf` outlines the whole `<tr>`. Hovering any other cell of that row (Kind, Status, dates) resolves `closest('[data-ask-tag]')` to the enclosing Card, so the "row" is pickable only over the title text even though the outline implies the full row. The row's visible text is also just the title, unlike the my-feedback `<li>` mark, which includes kind and status.
- Suggestion: Put the mark on the row (`FluentDataGrid` row attributes, or a wrapper), or narrow `boxOf` to the marked element for this case.
- Status: open

### Issue 8 — Severity: nit
- File: source/container-apps/web/projects/web-spa/source/features/ask-tag.ts:359
- Description: The `dataQa` fallback `target.closest("[data-qa]")` climbs past the pick root into the layout. A mark without its own `data-qa` therefore reports an ancestor's (possibly page-shell) data-qa as "its" data-qa. The chip disclosure then shows a misleading data-qa.
- Suggestion: Use only the element's own `data-qa`, or stop the climb at the pick root or the nearest marked ancestor.
- Status: open
