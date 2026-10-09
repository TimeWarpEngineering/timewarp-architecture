# Round 1 — merged findings
**Date:** 2026-10-10
**Sources:** general, tests, security

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 4 | 0 |
| suggestion | 0 | 7 | 0 |
| nit | 0 | 5 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor:171; features/application/modals/agent-ask/AgentAsk.razor:33-41,201-245
- Description: The UIAgent and transcript are component fields. AgentAsk is mounted only while the panel is open, and TimeWarpPage remounts on navigation, so Close and navigation both discard the transcript. close-ask-panel.cs Design says the next open continues the same conversation.
- Suggestion: Keep the transcript in a circuit-scoped store per ConversationGeneration (UIAgentOptions.Thread / IConversationThread + RestoreAsync), so a rebuilt agent restores it. Otherwise correct the Design regions.
- Source: general
- Disposition notes: Fixed. New app-scoped `AskConversationThreads` (registered in program.cs) hands out one in-memory `AskConversationThread : IConversationThread` per `ConversationGeneration`; a new generation gets an empty thread. Every UIAgent the panel builds gets that thread via `UIAgentOptions.Thread`. Decompiling 0.1.0-preview showed ChatPage's `AgentBoundary` never calls `RestoreAsync` and `MessageList` reads `Turns` only when its AgentContext changes, so AgentAsk now owns the `AgentContext` (CascadingValue + MessageList + MessageInput, mirroring ChatPage markup, keyed by the context) and awaits `AgentContext.RestoreAsync()` before rendering. `UIAgent.RestoreAsync` rebuilds the model `_history` too, so the model receives prior turns. Unanswered approval requests are dropped from the replay; answered ones show the recorded decision instead of buttons. Proof: `Rebuilt_Agent_Restores_Turns_And_Sends_The_History`, `Unanswered_Approval_Is_Not_Replayed_And_Answered_One_Reports_Its_Decision` (ask-conversation-thread-tests.cs), and Playwright asserts the answer is still shown after Close + reopen. Design regions updated (AgentAsk, close-ask-panel, new-conversation, agent-surface-state, design.md). Limitation: memory only; a page reload starts empty, and a turn cut off mid-stream (navigation) is not committed.

### M2 — Severity: bug — Status: fixed
- File: features/application/modals/agent-ask/AgentAsk.razor:139-145
- Description: EnsureAgentAsync rebuilds the agent when the route or edit mode changes. That wipes the transcript, and a pending approval card is orphaned under the disposed agent.
- Suggestion: Reuse the per-generation thread from M1 across rebuilds, and do not rebuild while a turn is in progress.
- Source: general
- Disposition notes: Fixed. Rebuilds reuse the per-generation thread from M1, so route/edit-mode rebuilds restore the transcript. EnsureAgentAsync skips the rebuild while `AgentContext.Status` is Streaming or AwaitingInput; a status subscription re-renders when the turn goes Idle/Error, which runs the deferred rebuild. Playwright toggles Automatically edit after an answer and asserts the turn is still on screen. Navigation still remounts TimeWarpPage (disposing the panel), which cancels an in-flight turn; that is documented in the AgentAsk/thread Design.

### M3 — Severity: bug — Status: fixed
- File: features/application/modals/agent-ask/AgentAsk.razor:215-221; source/features/ask-ai.ts:12-21
- Description: Typing `@` opens the menu, and choosing an entry then inserts a token that starts with `@`, giving `@@credential:<id>`.
- Suggestion: In InsertReference, replace an `@` that sits just before the caret instead of adding a second one.
- Source: general
- Disposition notes: Fixed. `InsertReference` (ask-ai.ts) replaces an `@` just before the caret when the token starts with `@`. Playwright drives the module in the browser: `see @` + `@profile:ada` -> `see @profile:ada`, and `see ` + token appends.

### M4 — Severity: bug — Status: fixed
- File: features/application/agent/web-mcp-dispatcher.cs (mode read at invoke); agent-surface-state.close-ask-panel.cs; agent-surface-state.set-edit-mode.cs
- Description: EditMode is global SPA state. Choosing Automatically edit in the in-app chat also removes the confirm bar for WebMCP mutating calls, and Close does not reset it, so Automatic outlives the conversation the person was watching.
- Suggestion: Reset EditMode to AskBeforeEditing on ClosePanel and say so in the Design regions. That keeps parity (both drivers read one mode) and bounds Automatic to an open panel.
- Source: security
- Disposition notes: Fixed. `CloseAskPanel` sets `EditMode = AskBeforeEditing`; Design regions updated in close-ask-panel, set-edit-mode, agent-surface-state, and design.md decision 3. Test `Close_Resets_Edit_Mode_But_Keeps_The_Conversation` (state reset, credential and generation kept, WebMCP prompts again after Close); Playwright asserts Ask before editing is pressed after Close + reopen.

### M5 — Severity: suggestion — Status: fixed
- File: source/features/ask-ai.ts:25-33; modals/agent-ask/AskAnswerBar.razor
- Description: Copy reads innerText of the whole chat root (every turn, approval cards and buttons). Thumbs use LastAnswer(), so copy and thumbs disagree about what the answer is.
- Suggestion: Copy the same last-answer text the thumbs use.
- Source: general
- Disposition notes: Fixed. JS export is now `CopyText(text)`; AskAnswerBar passes `LastAnswer()`, the same text the thumbs file. Playwright grants clipboard permissions, clicks Copy, and asserts the clipboard starts with the answer and excludes the prompt and button labels.

### M6 — Severity: suggestion — Status: fixed
- File: features/application/agent/catalog-agent-functions.cs:48,104-127
- Description: The approval wrapper is chosen when the functions are built. InvokeCoreAsync ignores current.RequiresApproval, so an unwrapped function built under Automatic keeps running with no prompt if the mode flips back to Ask mid-run.
- Suggestion: Refuse when current.RequiresApproval is true and this function was built without the wrapper.
- Source: security
- Disposition notes: Fixed. `CatalogAgentFunction` records whether it was built wrapped; when the re-selected tool requires approval and it was not wrapped it returns a failed `CatalogAgentCallResult` with `CatalogAgentFunctions.ApprovalRequiredError`. Test `Unwrapped_Function_Refuses_When_The_Mode_Returns_To_Ask`.

### M7 — Severity: suggestion — Status: fixed
- File: features/application/agent/agent-conversation-authority.cs:43-49
- Description: The principal check fails open when the current user has no parseable NameIdentifier.
- Suggestion: When a credential is present, require a parseable id that matches it.
- Source: security
- Disposition notes: Fixed. With a credential present, `Denial` requires a parseable NameIdentifier guid equal to the credential principal; a missing user, missing claim, or unparseable id returns PrincipalError. Tests updated (nameless/garbled/null user now refuse). Design regions and design.md decision 4 updated.

### M8 — Severity: suggestion — Status: fixed
- File: modals/agent-ask/AgentAsk.razor:96-105; agent-conversation-credential.cs / issuer Design
- Description: Scopes are copied from all current permission claims, and a null credential is allowed. The credential is a client-side guardrail, not a security boundary, and the Design does not say so.
- Suggestion: State in the Design regions that it is advisory, that null means unbounded, and that server [EndpointAuthorize] is the boundary.
- Source: security
- Disposition notes: Fixed (docs). AgentAsk, agent-conversation-credential, issuer, and authority Design regions plus design.md now say the credential is an advisory client-side guardrail, scopes copy every current permission claim, null means unbounded, and server [EndpointAuthorize] is the boundary.

### M9 — Severity: suggestion — Status: fixed
- File: tests/.../catalog-agent-parity-tests.cs:272
- Description: Both sides start from SelectAsync with an explicit mode. The real publish path (WebMcpPublisher.PublishAsync reading the store's EditMode) is not exercised.
- Suggestion: Add a case that sets the mode on AgentSurfaceState, runs PublishAsync against a recording IWebMcpModelContext, and compares the result with the in-app list.
- Source: tests
- Disposition notes: Fixed. `WebMcpPublisher.PublishAsync(IWebMcpModelContext?, CancellationToken)` overload (the parameterless form passes the JS context). Test `Published_WebMcp_List_Reads_The_Store_Edit_Mode_And_Matches_In_App` sets the mode on AgentSurfaceState, publishes to a recording context, and compares names and approval bits with the in-app list for both modes.

### M10 — Severity: suggestion — Status: fixed
- File: tests/.../catalog-agent-parity-tests.cs:161-209
- Description: The ask-mode expired-credential refusal is tested only for WebMCP. A successful in-app automatic run is not asserted.
- Suggestion: Add the in-app ask-mode expired case and a positive in-app automatic run.
- Source: tests
- Disposition notes: Fixed. Test `In_App_Ask_Mode_Expired_Refuses_After_Approval_And_Automatic_Run_Completes`: the ask-mode wrapped function refuses with ExpiredError after approval, and an automatic run with a valid credential completes (Counter 10 -> 15).

### M11 — Severity: suggestion — Status: fixed
- File: tests/.../ask-surface-playwright-tests.cs:145
- Description: The Playwright checks are mostly presence checks. The edit mode is never toggled, and DismissPrivacyNotice has no state test.
- Suggestion: Toggle the edit mode and assert aria-pressed. Add a state-level DismissPrivacyNotice test and a ClosePanel mode-reset test (M4).
- Source: tests
- Disposition notes: Fixed. Playwright toggles Automatically edit and asserts aria-pressed on both buttons, then asserts the reset after Close + reopen. State tests `Dismiss_Privacy_Notice_Hides_It_For_The_Session` and `Close_Resets_Edit_Mode_But_Keeps_The_Conversation`.

### M12 — Severity: nit — Status: fixed
- File: modals/agent-ask/AgentAsk.razor:110-124; agent-conversation-credential-issuer.cs Design
- Description: The display name is never set from the first prompt, so the title is always the default. The issuer Design says otherwise.
- Suggestion: Reword the Design to say the name is the default label and is not derived from the prompt.
- Source: general
- Disposition notes: Fixed (docs). Issuer Design now says the display name is the default label, not derived from the prompt; design.md decision 4 says the same.

### M13 — Severity: nit — Status: fixed
- File: modals/agent-ask/AgentAsk.razor:74-83
- Description: After the credential expires, the panel does not say why tool calls are refused.
- Suggestion: Show an expired line with New conversation as the remedy.
- Source: general
- Disposition notes: Fixed. Panel header shows `data-qa=AskConversationExpired` ("This conversation has expired, so page actions are refused. Choose New to start another.") when `AgentConversationAuthority.IsExpired` is true; IsExpired is unit-tested. Not covered in Playwright (the browser lane cannot plant an expired credential without a hook); it re-evaluates on render, not on a timer.

### M14 — Severity: nit — Status: fixed
- File: tests/.../catalog-agent-parity-tests.cs:92,320
- Description: Credentials are issued at the real UtcNow, and expiry is capped at the next UTC midnight, so a run just before midnight can flake.
- Suggestion: Use a fixed mid-day `now` wherever Denial takes utcNow.
- Source: tests
- Disposition notes: Fixed. Pure Denial checks use a fixed noon `now`; credentials meant to be valid in invoke tests are issued at `FutureNoon()` (mid-day tomorrow UTC), and the expired one three days before that, so the UTC-midnight cap cannot flake a run.

### M15 — Severity: nit — Status: fixed
- File: modals/agent-ask/AskAnswerBar.razor:71; agent-surface-state.load-chat-configuration.cs
- Description: SupportUrl is rendered into href with no scheme check. It is a constant today.
- Suggestion: On load, accept a `/`-relative path (not `//`) or an http/https URL; otherwise fall back to the default.
- Source: security
- Disposition notes: Fixed. New `AskSupportLink.Normalize`: `/`-relative (not `//` or `/\`) or absolute http/https, else `XaiChatDefaults.SupportUrl`; LoadChatConfiguration applies it. Tested in `Edit_Mode_Credential_And_Panel_State_Follow_The_Shared_Rules`.

### M16 — Severity: nit — Status: fixed
- File: features/application/agent/ask-resource-references.cs:86
- Description: The `@profile:{alias}` token carries the alias verbatim, including whitespace and control characters.
- Suggestion: Strip whitespace and control characters from token values.
- Source: security
- Disposition notes: Fixed. `AskResourceReferences.TokenValue` strips whitespace and control characters from credential id, profile alias, and site-settings version tokens (empty after stripping = skipped; labels keep the text). Tested with tabs, newlines, and a bell character.

## Duplicates / conflicts

- general #2 and security #1 overlap (edit mode scope). Collapsed: the rebuild/transcript part is M2, the WebMCP blast radius is M4.
- general #6 and security #4 both touch credential lifetime. They are kept separate: M13 is UI, M8 is documentation.
- tests #4 is narrowed to M11. Clicking copy/thumbs and asserting "See reasoning" in Playwright need a configured model, which the Playwright lane does not have.
