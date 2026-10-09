# Round 2 — general
**Date:** 2026-10-10
**Scope reviewed:** fix commit 2655f37e0 (re-verify M1–M16 + new defects on the fix delta)

## Prior findings
| ID | Status | Note |
|----|--------|------|
| M1 | fixed | AskConversationThreads (scoped, keyed by generation) feeds UIAgentOptions.Thread; the panel owns AgentContext and awaits RestoreAsync before MessageList attaches. Design regions match. Thread-level tests exercise restore and history. See N1 (dispose race) and N3 (weak browser proof). |
| M2 | fixed | Rebuild reuses the generation thread and is deferred while Streaming/AwaitingInput; OnStatusChanged re-renders on Idle/Error. See N2 and N3. |
| M3 | fixed | InsertReference replaces an @ just before the caret; browser test drives the module directly. |
| M4 | fixed | CloseAskPanel resets EditMode to AskBeforeEditing; Design regions updated; Close+reopen happens inside AssertDockedPanelAsync, so the Playwright assertions are valid. |
| M5 | fixed | CopyText(LastAnswer()) matches the thumbs text. |
| M6 | fixed | CatalogAgentFunction records Wrapped and refuses with ApprovalRequiredError when the re-selected tool now needs approval. |
| M7 | fixed | Denial now requires a parseable NameIdentifier equal to the credential principal when a credential is present; fail closed. |
| M8 | fixed | Advisory-guardrail wording present in the Design regions. |
| M9 | fixed | PublishAsync(IWebMcpModelContext?, ct) overload added; parameterless form delegates to it. |
| M10 | fixed | In-app ask-mode expired and automatic-run cases added. |
| M11 | fixed | aria-pressed toggles and the post-Close reset are asserted. Weaker than claimed for the rebuild (N3). |
| M12 | fixed | Docs reworded. |
| M13 | fixed | Expired line shown from IsExpired on render; documented as not timer-driven. |
| M14 | fixed | Fixed noon / FutureNoon clocks. |
| M15 | fixed | AskSupportLink.Normalize rejects //, /\, and non-http(s) schemes; applied in LoadChatConfiguration. |
| M16 | fixed | TokenValue strips whitespace and control characters; labels keep the text. |

## Summary
All sixteen round-1 findings are fixed in code and the Design regions match the new behavior. The new hand-built ChatPage replacement has no subscription leak in the normal path and keeps side effects out of the component (it only reads state and awaits AgentContext.RestoreAsync). There is one real race on disposal and two smaller gaps in deferral behavior and test strength.

## Issues
### N1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor (EnsureAgentAsync, ~lines 187-250; Dispose)
- Description: EnsureAgentAsync awaits GetAuthenticationStateAsync, CatalogAgentToolSet.SelectAsync, and conversation.RestoreAsync before assigning Agent, Conversation, Thread, and StatusSubscription. Navigation remounts TimeWarpPage and disposes the panel while Building is true. Dispose calls ReleaseAgent, which sees nothing, and then the in-flight build assigns a new UIAgent, AgentContext, Functions, and status subscription on a disposed component. Nothing disposes them afterwards, and OnStatusChanged then calls InvokeAsync(StateHasChanged) on a disposed component.
- Suggestion: Track a Disposed flag set in Dispose. After the awaits (before assigning), if Disposed, dispose the locals (conversation, agent, client, functions) and return. Guard OnStatusChanged with the same flag.
- Status: open

### N2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor (EnsureAgentAsync TurnInProgress guard)
- Description: The deferral also applies to the generation change. With a pending approval card (AwaitingInput), New conversation bumps the generation and the credential but the old transcript and card stay on screen until the person answers the card, so New appears to do nothing. The Design region says the rebuild waits, but does not call this case out.
- Suggestion: Let a generation change bypass the TurnInProgress guard (the old turn is being abandoned deliberately), or disable New while a turn is in progress. At minimum, state the behavior in the Design region.
- Status: open

### N3 — Severity: nit
- File: tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs:183-192
- Description: The "turn survives an edit-mode rebuild" check waits for AgentAskStarting to be Hidden immediately after the click. That passes at once if the rebuild has not yet started (the status span is not yet rendered), and the answer locator was already visible before the click, so a regression that wiped the transcript could still pass. The rebuild-defer path (M2) has no unit-level test at all.
- Suggestion: After the toggle, wait for a sentinel that proves the rebuild happened (for example, a BuiltMode data attribute or aria state), then assert the answer. Alternatively, add a bUnit-style or thread-level test of the deferral.
- Status: open
