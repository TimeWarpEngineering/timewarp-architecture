# Ask rejection does not poison the turn with retry loop

## Description

After rejecting an approval the Ask conversation falls into "Something went
wrong. Please try again." and Retry repeats the error. Reported 2026-10-28:
after rejecting Screenshot.CaptureScreenshot ("show me the Filter bar" turn)
every later "why didn't you find the route?" turn errored and Retry looped.

## Goal (implementer restatement)

When someone rejects an Ask approval card (for example Screenshot.CaptureScreenshot), the
conversation must keep working. The rejection turn finishes with a model answer, later turns do
not fall into the MessageList Error state, and Retry has no stored failure to replay.

Acceptance:

- A test drives the product Ask path (UIAgent + AgentContext + AskConversationThread over
  CatalogAgentSession.CreateInvokingClient and RelayChatClient). It rejects a real
  Screenshot.CaptureScreenshot call, then sends a follow-up question. Every relay request passes
  CompleteAgentChat.Validator, the context never reaches Error, and the model sees the decline.
- AskConversationThread records the rejection, so ApprovalAnswer reports false and a rebuilt
  agent restores the turn and can keep talking.
- The test fails before the fix and passes after it.

## Checklist

- [x] Repro with the scripted approval loop over CatalogAgentSession
  .CreateInvokingClient: reject a Screenshot.CaptureScreenshot call, capture
  the model/relay error that puts MessageList in Error.
- [x] Fix so a rejection completes the turn gracefully (model sees the decline
  and answers) and Retry does not replay the same failure; record the decision
  in AskConversationThread so the restored card shows Rejected.
- [x] Cover with a catalog-agent approval test (reject then continue the
  conversation without Error).

## Session

- Created: ses_e7be8301fffeuQA2WOJDPvWuQh (2026-10-28)
- 2026-10-30 implementer (ganda task work, claude-opus-5-5): repro, fix, test, gates.

## Notes

Evidence: AgentAsk.razor Reject is approval.Reject("Declined.") on a
FunctionApprovalBlock; the error UI + Retry is the library MessageList error
state, not a template string (no "Something went wrong" in web-spa). Suspect
the declined tool call errors against xAI/relay and the turn never completes,
so CompleteTurn never commits and Retry resends the failing request. Check
FunctionInvokingChatClient + RelayChatClient error mapping and whether the
rejection needs a follow-up model call.

## Results

**Root cause.** The rejection turn itself worked: the model got a rejected tool result and
answered. The next turn failed. When Microsoft.Extensions.AI 10.11.0 rejects an approval,
FunctionInvokingChatClient sets the call's FunctionCallContent.InformationalOnly = true. That
FunctionCallContent object is shared with the ToolApprovalRequestContent and
ToolApprovalResponseContent that UIAgent keeps in its history and in AskConversationThread.
FunctionInvokingChatClient only strips approval pairs whose call is not informational. So on
every later turn the rejected request (an assistant message) and the response (a user message)
reached RelayChatClient. Each became an empty Turn. web-server's CompleteAgentChat.Validator
returned `400 One or more validation errors occurred.: A turn needs text, a tool call, or a tool
result.` twice. RelayChatClient threw that error, and AgentContext went to Error. Retry replays
the same history, so it failed the same way. Approved pairs were not affected, because their calls
stay non-informational and the client strips them.

**Fix.** `RelayChatClient.ToTurns` sends only messages that reach the model: text, a tool call,
a tool result, or a user picture (`ReachesModel`). Approval request and response messages stay in
the Ask history, so the restored card still reads its decision, but they are never sent. The
model still sees the rejected call and its "rejected … Declined." result. The Design region
records why.

**Decision record.** No change was needed. AskConversationThread already commits the
ToolApprovalResponseContent with the rejection turn, and AgentAsk maps
`ApprovalAnswer(...) == false` to Rejected. The new test asserts this before and after a rebuild.

**Files**

- `source/container-apps/web/projects/web-spa/features/application/agent/relay-chat-client.cs`:
  adds the `ReachesModel` filter and its Design note.
- `tests/container-apps/web/web-spa-integration-tests/features/application/ask-rejection-tests.cs`
  (new, Unit, no Aspire). It drives UIAgent + AgentContext + AskConversationThread over
  CatalogAgentSession.CreateInvokingClient(RelayChatClient). The tools come from the real catalog
  selection on /Settings. The web-server stand-in runs CompleteAgentChat.Validator on every
  request. The test rejects Screenshot.CaptureScreenshot, asks a follow-up, retries after a
  transient 502, and then restores in a rebuilt agent and keeps talking. A second test checks that
  the relay leaves answered approval messages out of the command.
- `catalog-agent-screenshot-tests.cs`: CaptureSpa and CaptureJsRuntime are now `internal`, so the
  new class can reuse them without the Aspire session SetupOnce.

**Proof.** Before the fix both new tests failed with the 400 above, on the follow-up turn. After
the fix both pass. `--filter-tag Unit` passes 428/428. `ganda repo audit` passes, with one
advisory kanban-landed warning about other tasks' claims and nothing on this task. `dev build`
has 0 warnings and 0 errors. `dev template-smoke` (default variant) succeeded. The Aspire
integration tests in CatalogAgent_Should were not run locally, because workers do not start an
AppHost. CI runs them.

### How to validate

Smoke:

```bash
cd tests/container-apps/web/web-spa-integration-tests
dotnet build -v q
dotnet run --no-build -- --filter-class AskRejection
```

Expect: `total: 2 failed: 0 succeeded: 2`. Revert the `history.FindAll(ReachesModel)` line in
relay-chat-client.cs and rerun. Both tests then fail with `400 One or more validation errors
occurred.: A turn needs text, a tool call, or a tool result.`

Manual check (live Ask): on a page with a capture tool, ask "show me the Filter bar" and click
Reject. The assistant answers that it was declined. Then ask "why didn't you find the route?".
It answers, with no "Something went wrong" and no Retry loop. Close and reopen Ask: the card
shows Rejected.
