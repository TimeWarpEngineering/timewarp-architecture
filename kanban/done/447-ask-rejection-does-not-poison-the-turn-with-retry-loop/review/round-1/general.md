# Round 1 — general
**Date:** 2026-10-30
**Scope reviewed:** commit b32495fc5 product files

## Summary

The change filters the history in `RelayChatClient.ToTurns` down to messages the model reads (text, tool call, tool result, user image), so the approval request/response pair that survives a rejection no longer becomes an empty `Turn` that fails `CompleteAgentChat.Validator`. I found no defects: the filter matches the validator's `IsEmpty` rule for every content kind `FunctionInvokingChatClient` produces, the picture-turn and `lastUser` logic still behave correctly on the filtered list, and the new test drives the real product path and covers every acceptance item. The remaining points are maintainability suggestions: the filter predicate duplicates knowledge held in `ToTurn` and the validator, and the new Unit test borrows helpers from an Integration-tagged test class.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/agent/relay-chat-client.cs:161
- Description: `ReachesModel` re-states, per content type, what `ToTurn` (line 190) turns into a non-empty `Turn` (text, `FunctionCallContent`, `FunctionResultContent`, user-role image `DataContent`) and what the validator's private `IsEmpty` (complete-agent-chat-contracts.cs:233) accepts. Nothing keeps the three in agreement. If `ToTurn` later maps a new content kind (for example a user audio or PDF `DataContent`, or another result shape) and `ReachesModel` is not updated, that message is silently dropped. If `ToTurn` stops mapping a kind that `ReachesModel` still accepts, the empty-turn 400 comes back. AGENTS.md hard rule: when two things must agree, generate one from the other or add a build check.
- Suggestion: Derive the filter from the produced turn instead of a parallel predicate. For example, build `(message, turn)` pairs with `ToTurn`, and keep a pair only when the turn has non-whitespace text, images, tool calls, or a tool call id or result. `lastUser` and the picture-turn loop then run over the kept pairs. A shorter step: add a unit test for the edge kinds (reasoning-only assistant message, whitespace-only text, non-image user `DataContent`, image-only user message) so a drift fails a test.
- Status: open

### Issue 2 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/ask-rejection-tests.cs:27
- Description: The Unit-tagged `AskRejection_Should` reaches into `CatalogAgent_Should` (declared in catalog-agent-tests.cs:46 as `[TestTag("Integration")]` with an Aspire `SetupOnce`) for `CaptureSpa` and `CaptureJsRuntime`, which this commit widens from `private` to `internal`. This works: the nested types do not touch the class's setup, and the Design note in catalog-agent-screenshot-tests.cs:18 records the reason. It does couple a Unit test to a helper that lives inside an Integration class, and it duplicates the `"Screenshot.CaptureScreenshot"` literal (here at line 27; the other copy is the private `CaptureTool` const in catalog-agent-screenshot-tests.cs:39).
- Suggestion: Optional. Move `CaptureSpa` and `CaptureJsRuntime` to the `infrastructure/` folder (next to `aspire-spa-test-application.cs`) as top-level internal types. Both tests then share them without borrowing from an Integration class. Not needed to merge.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent/relay-chat-client.cs:114
- Description: Filtering before `FindLastIndex` changes what `lastUser` means. It used to be the last user-role message, including a surviving approval-response message, which is user-role. It is now the last message that reaches the model, so the person's message. The new meaning matches the Design text ("answers a call made after the person's last message"). In a turn with a rejection, tool pictures from calls made earlier in that same turn are now kept where before they fell before the approval-response message and were dropped. This is more correct, but the Design note at lines 24-29 does not mention it, and no test pins it.
- Suggestion: Add one clause to the Design note (`lastUser` is computed after filtering), and optionally a case in `Relay_Leaves_Answered_Approvals_Out_Of_The_Command` or the screenshot tests that puts a tool-image result before a surviving approval pair and asserts the picture turn is still sent.
- Status: open

## Checked, no defect

- `ReachesModel` against the validator `IsEmpty` rule: whitespace-only text matches (`IsNullOrWhiteSpace` on both sides). A message that holds only `TextReasoningContent` is dropped; `ChatMessage.Text` excludes reasoning, so it was an empty turn before too. A non-image user `DataContent` or an assistant `DataContent` is dropped, and it was an empty turn before too. A user image-only message is kept. Messages with whitespace text plus a call or result are kept.
- Picture-turn placement: `runEnds` looks at the next message in the filtered list, so a dropped approval message between tool turns cannot split or merge a tool run. A dropped message never carried a `FunctionResultContent`.
- Test acceptance: the first test rejects the real `Screenshot.CaptureScreenshot` (`ApprovalRequiredAIFunction` asserted at line 47). It validates every relay request through `CompleteAgentChat.Validator`, asserts `Idle` and no `Failures` after each turn, and asserts the browser is never reached, the tool result contains "rejected" and "Declined.", and `ApprovalAnswer(...) == false` before and after the rebuild. It also covers the follow-up turn, the Retry after a scripted 502, and a rebuilt agent that restores and keeps talking. The second test pins the role sequence `user|assistant|tool|assistant|user` for the later-turn shape.
- Hygiene: the file name is kebab-case, Purpose and Design regions are present, only Jaribu and Shouldly are used, the class is Unit-tagged with a `ModuleInitializer` registration like the sibling Unit tests, and no estimates appear. Making `CaptureSpa` and `CaptureJsRuntime` internal widens visibility only to the test assembly. The `CaptureSpa` constructor takes `CaptureJsRuntime`, both internal, so there is no accessibility mismatch.
