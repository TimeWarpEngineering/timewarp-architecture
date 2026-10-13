# Round 2 — general
**Date:** 2026-10-13
**Scope reviewed:** fix commit 657470909 + re-verification of round-1 M1–M8

## Summary
All eight round-1 findings are fixed in the code, and the state, chat-client and JS teardown paths show no regressions (MarkSent waits for success and carries the sent ids, instructions are rebuilt per request on a cloned ChatOptions, LocationChanged is unsubscribed in Dispose, and handler-side `ReRenderSubscribers` matches the NotificationState handlers). One new question: the M3 keyboard exemption may block the keyboard-only path when focus is still in the panel after Tag, and the Playwright test blurs focus first, so it does not cover that case. There is also one nit about region text.

## Prior findings
| ID | Status | Note |
|----|--------|------|
| M1 | fixed | `ask-tag-chat-client.cs`: `GetResponseAsync` reports after the response. Streaming reports on the first update, or at the end of an empty stream. An exception or cancellation before the first update reports nothing. The `MarkSent` handler moves only `action.Ids` and keeps chips attached after the snapshot. Ids are unique because `NextId` only resets in `Initialize`. If the stream throws after the first update, the chips are already marked sent. The tags did go out, and the Design region documents this, so it is acceptable. `Keep_Chips_Attached_When_The_Request_Fails` covers both the sync and the streaming failure. |
| M2 | fixed | `Refresh` clones the options and sets `Instructions = RenderInstructions()` on each outer request. It runs before the first await, on the caller's context, and `MarkSent` is still marshalled through `InvokeAsync`. The tag client sits outside the tool-invoking client, so one refresh covers the whole tool loop. Both the integration test and the Playwright second-turn check assert it. |
| M3 | fixed (see N1) | Enter and Tab pass through natively when `insideAsk(event.target)` is true, and Esc always cancels. Space was never intercepted, so the Design text "Enter, Space and Tab … stay native" is accurate. |
| M4 | fixed | `SessionChanged` calls `CancelPickAsync` only if a pick was active. The stale `StartPick` returns because `PickGeneration` was bumped. AgentAsk subscribes to LocationChanged in `OnInitialized` and unsubscribes in `Dispose`, as `WebMcpAgentSurface` and `NavigationListener` do. `Dispose` cancels an active pick. `root()` is looked up again on every use. |
| M5 | fixed | `[TrackAction]` is removed. The panel and the chips read `IsPicking`. `subscriptions.ReRenderSubscribers<AskTagState>()` follows the existing NotificationState handlers. `CancelPick` / `SessionChanged` / the end of `StartPick` re-render through the normal post-processor. |
| M6 | fixed | The Playwright test now asserts the payload heading and token in the last user message of the completions POST. It also checks that the private and aria-hidden probes are never reached by Tab or hover, that Tab+Enter chooses, that Enter on Cancel cancels, and that the second turn sends no payload but its instructions name the tag. |
| M7 | fixed | `candidateAt` maps a hit inside a `tr` / `[role=row]` to the mark in that row. `describe` reads its text from `boxOf(target)`, which is the whole row, and private or hidden cells are still skipped by `visibleText`. All current row marks (the two Feedback list pages) have one mark per row, so the first-mark-wins rule hides no nested mark. |
| M8 | fixed | `describe` uses only `target.getAttribute("data-qa")`. |

## Issues
### N1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/source/features/ask-tag.ts:234
- Description: The Enter and Tab exemptions apply whenever focus is inside `.twe-agent-ask`. A keyboard-only user starts pick mode by pressing Enter on the Tag button, which is inside the panel. If focus stays in the panel, Tab moves focus natively instead of moving through the candidates. Enter presses whatever is focused instead of choosing. Only the arrow keys still move the highlight. Meanwhile the hint says "Tab or arrows to move, Enter to choose". Focus may not stay there: once the Tag button turns `Disabled`, the browser may move focus to body, in which case the path works. That depends on how the FluentButton web component handles focus. The Playwright keyboard path blurs `document.activeElement` before pressing Tab (`CycleHintsAsync`, ask-sign-in-playwright-tests.cs:489), so this case is never exercised.
- Suggestion: Add a Playwright step that focuses Tag, presses Enter, then presses Tab and Enter without the blur. If that fails, narrow the exemption. For example, keep Enter/Tab native only in the composer textarea and on the `AskTagPicking` status row (the Cancel button). Or have `PickSession` move focus out of the panel when it starts, so Esc and the arrows remain the way back.
- Status: open

### N2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:51
- Description: The Design text says the same thing twice. The new sentence (lines 47–50, "Closing the panel, disposing it, and any location change cancel pick mode") repeats the old line 51, "Closing the panel closes the picker and cancels pick mode." Several reflowed Design lines are also much longer than the surrounding wrap width: `ask-tag-state.start-pick.cs:10`, `ask-tag.ts:24`, `ask-tag-chat-client.cs:16`, `AskTagChips.razor:10`. The content is accurate (TWA0004 is satisfied); this is cosmetic.
- Suggestion: Drop line 51 and rewrap those regions.
- Status: open
