# Round 1 — security
**Date:** 2026-10-10
**Scope reviewed:** branch vs master

## Summary
No XSS or injection path found: no MarkupString/innerHTML, ask-ai.ts writes `textarea.value` and reads `innerText` only, and the config contract keeps its `[EndpointAuthorize]` policy with no key exposure. The conversation credential is a client-side value (clock, scopes and state are all in the user's browser), so it is a UX guardrail and not a security boundary. That matches the design, and server `[EndpointAuthorize]` stays the gate. The real concern is approval parity: Automatic edit mode is one global SPA flag that also disables approval for WebMCP callers, and the in-app approval wrapper is fixed at build time.

## Issues
### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs:~195 (mode read in FindOfferedAsync); agent-surface-state.set-edit-mode.cs; agent-surface-state.close-ask-panel.cs
- Description: `AgentSurfaceState.EditMode` is a global store value, not tied to the in-app conversation. WebMCP (a third-party or browser-extension agent) reads it at invoke time, so when the user picks "Automatically edit" for the in-app chat, every WebMCP mutating call also skips the confirm bar. ClosePanel does not reset the mode (only NewConversation does), so Automatic persists after the panel is closed and the user is no longer watching. The design says Automatic is "for this conversation only", but WebMCP callers are not part of that conversation. Permission filtering still applies, so this does not let an unauthorized user act. The weakening is for an agent the user did not knowingly give blanket consent to.
- Suggestion: Either (a) have WebMCP always compute the approval bit with AskBeforeEditing, or honour Automatic only while the panel is open with a live, unexpired credential, or (b) reset EditMode to AskBeforeEditing on ClosePanel. Pick one and state it in the Design regions and the parity test.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-functions.cs:98-125
- Description: The approval wrapper is chosen when the function list is built (`tool.RequiresApproval ? new ApprovalRequiredAIFunction(...)`), but InvokeCoreAsync re-selects with the current mode and ignores `current.RequiresApproval`. If the user flips Automatic back to Ask while an agent run is in flight, the old agent still holds unwrapped functions, so later mutating calls run with no prompt. The panel rebuilds only on the next render/ensure cycle (`BuiltMode` check, AgentAsk.razor:142).
- Suggestion: In CatalogAgentFunction.InvokeCoreAsync, if `current.RequiresApproval && !Tool.RequiresApproval`, return a refusal result (for example "Edit mode changed; ask again"). Defense in depth, small change.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/agent/agent-conversation-authority.cs:43-49
- Description: The principal check fails open. It mismatches only when the current user has a parseable NameIdentifier GUID. If a credential exists and the current principal has no such claim (or an unparseable one), the check passes. The design documents this, but it contradicts "bound to principal". Impact is limited because tool selection and server authorization still apply.
- Suggestion: When a credential is present, require a parseable id and a match, else return PrincipalError.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:96-105
- Description: Scopes are copied from all of the user's current permission claims at issue time, so the scope check never narrows what the principal can already do; the only effect is that a permission granted after issue is refused for the old conversation. A null credential is also allowed, so WebMCP use before the panel is ever opened has no expiry. This does not give the "short-lived scoped" guarantee the name suggests.
- Suggestion: State in the credential Design region that it is an advisory client-side guardrail and that null means unbounded.
- Status: open

### Issue 5 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskAnswerBar.razor:71
- Description: `href="@SupportUrl"` takes server-provided config text (agent-surface-state.load-chat-configuration.cs:49) with no scheme check. Blazor does not sanitize hrefs, so a `javascript:` value would run on click. Today it is a compile-time const (`/Feedback`), so there is no live exploit. The design calls it a template setting.
- Suggestion: On load, accept only a relative path starting with `/` (not `//`) or an http/https URL, else fall back to the default.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent/ask-resource-references.cs:86
- Description: `@profile:{alias}` inserts the user's own alias verbatim into the prompt, including spaces or newlines. This is self-injection only (the user's own data, and the user presses send); tokens are never parsed or trusted server-side.
- Suggestion: Optionally strip control characters and whitespace from the token value.
- Status: open
