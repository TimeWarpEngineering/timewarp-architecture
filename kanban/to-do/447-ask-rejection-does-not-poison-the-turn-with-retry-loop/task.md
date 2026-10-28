# Ask rejection does not poison the turn with retry loop

## Description

After rejecting an approval the Ask conversation falls into "Something went
wrong. Please try again." and Retry repeats the error. Reported 2026-10-28:
after rejecting Screenshot.CaptureScreenshot ("show me the Filter bar" turn)
every later "why didn't you find the route?" turn errored and Retry looped.

## Checklist

- [ ] Repro with the scripted approval loop over CatalogAgentSession
  .CreateInvokingClient: reject a Screenshot.CaptureScreenshot call, capture
  the model/relay error that puts MessageList in Error.
- [ ] Fix so a rejection completes the turn gracefully (model sees the decline
  and answers) and Retry does not replay the same failure; record the decision
  in AskConversationThread so the restored card shows Rejected.
- [ ] Cover with a catalog-agent approval test (reject then continue the
  conversation without Error).

## Session

- Created: ses_e7be8301fffeuQA2WOJDPvWuQh (2026-10-28)

## Notes

Evidence: AgentAsk.razor Reject is approval.Reject("Declined.") on a
FunctionApprovalBlock; the error UI + Retry is the library MessageList error
state, not a template string (no "Something went wrong" in web-spa). Suspect
the declined tool call errors against xAI/relay and the turn never completes,
so CompleteTurn never commits and Retry resends the failing request. Check
FunctionInvokingChatClient + RelayChatClient error mapping and whether the
rejection needs a follow-up model call.
