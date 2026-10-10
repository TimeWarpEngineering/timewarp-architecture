# Round 1 — general
**Date:** 2026-10-10
**Scope reviewed:** branch vs master

## Summary
The credential authority, tool-set/approval parity, and state actions are coherent and match design.md. The defects are in the panel component: it is mounted only while open, and it rebuilds its agent on mode and route changes, so the transcript does not survive the lifetimes the Design regions promise. There are also a doubled `@` on tag insertion, a Copy that grabs more than the answer, and a conversation title that is never set.

## Issues
### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor:171 (with features/application/modals/agent-ask/AgentAsk.razor:127-209 and agent-surface-state.close-ask-panel.cs Design)
- Description: `<AgentAsk />` is rendered only under `@if (AgentSurfaceState.IsPanelOpen)`, and the `UIAgent`/transcript are component fields (`Agent`, `Functions`, disposed in `Dispose`). Closing the panel therefore disposes the agent and discards the transcript. `TimeWarpPage` also remounts on navigation, which does the same. The credential and generation survive in the store, but the transcript does not. close-ask-panel.cs Design says "the credential, edit mode, and transcript generation stay so the next open continues the same conversation", and the AgentSurfaceState Design justifies shell state by remount. After Close then Ask AI, the header still shows the old conversation while the chat is empty.
- Suggestion: Keep AgentAsk mounted while closed (hide it with CSS, `display: contents` or `none`), or hold the agent in a scoped service so it survives remounts. Otherwise correct the Design regions: closing starts a new transcript, and the credential persists without it.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:139-145
- Description: `EnsureAgentAsync` rebuilds the `UIAgent` whenever `BuiltPath` or `BuiltMode` differs. Toggling "Automatically edit" or navigating therefore discards the conversation, which the credential and generation say is the same one. If a tool-approval card is pending, the agent is disposed under it and the call is orphaned. The edit mode applies to a "conversation", yet flipping it mid-conversation resets that conversation. design.md decision 3 says only that "the panel rebuilds its tools". It does not say the transcript is dropped.
- Suggestion: Keep one agent per `ConversationGeneration`. Resolve the approval bit at invoke time, as the WebMCP dispatcher does, and read the path and mode live in the function. Or, at minimum, document the reset and disable the toggle while an approval is pending.
- Status: open

### Issue 3 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:215-221 and source/container-apps/web/projects/web-spa/source/features/ask-ai.ts:12-21
- Description: Typing `@` opens the menu from `OnKeyDown`, which runs before the character lands in the textarea. `InsertReference` then splices the full token (which starts with `@`) at the caret without removing the typed `@`. Result: `@@credential:<id>`. The Tag button path (no typed `@`) is fine. design.md decision 5 says "Typing `@` opens that menu. Choosing an entry inserts the token".
- Suggestion: In `InsertReference`, if the character before the caret is `@`, replace it instead of inserting after it. Also handle the case where the menu is cancelled with `@` left in the input.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/source/features/ask-ai.ts:25-33 and features/application/modals/agent-ask/AskAnswerBar.razor
- Description: `CopyAnswer` copies `innerText` of the whole `.sc-ai-root`: every turn, prompts, approval cards, buttons, and the status line. The Design says it "reads the rendered answer text", and design.md says each answer has copy and thumbs. The bar is rendered once in `MessageListContent`, and thumbs use `LastAnswer()`. Copy and thumbs do not agree on what "the answer" is, and Copy is not scoped to an answer.
- Suggestion: Copy the last answer, using the same source as `LastAnswer()` (the `RichContentBlock` raw text). Pass the text from C# to the module, or scope the selector to the last response element. Clarify per-answer versus last-answer in the Design.
- Status: open

### Issue 5 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:110-124
- Description: `Issue` is never given a `displayName`, so `ConversationDisplayName` is always "Ask AI conversation". The issuer's Design says the display name "is the conversation title, truncated so a long first prompt cannot dominate the header", but nothing sets it from the first prompt, and `MaxDisplayNameLength` truncation is dead in practice. The "New conversation" fallback shows only until the credential is issued.
- Suggestion: Either set the title from the first user message, or reword the Design/issuer comment to say the name is a constant.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:74-83
- Description: `IssueConversationAsync` mints once per generation and never re-mints. After expiry (at most 12 hours, or the next UTC midnight), every mutating tool call returns the "expired" message while the panel shows no indication. The only recovery is New conversation. This matches design.md decision 4 on refusing, but nothing in the UI tells the person why.
- Suggestion: Show an "expired, start a new conversation" line in the header when `Conversation.ExpiresAt <= now`, or re-issue on next use.
- Status: open
