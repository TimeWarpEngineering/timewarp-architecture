# Design: Ask AI side panel, edit modes, and WebMCP parity (292)

Compared with what task 271 shipped (catalog-to-tools mapping, Ctrl-K Ask modal, WebMCP registration, in-app approval). Each Cloudflare "What to take" item is adopted, adapted, or rejected before the code that follows it.

## What 271 already shipped

- One catalog selector (`CatalogAgentToolSet`) feeds the in-app `AIFunction` list and the WebMCP publisher.
- Page scope comes from the current route (`PageAgentScope`). Human-only actions are dropped by visibility. Permissions filter the principal.
- Mutating tools wrap `ApprovalRequiredAIFunction`. Read-only allow-list: `Credentials.FetchCredentials`, `Feedback.ListMyFeedback`, `Feedback.OpenFeedback`.
- Ask lived in a Ctrl-K modal (`AgentAsk`). The page did not reflow. There was no top-bar button.
- The model calls the SPA through the signed-in session. `[EndpointAuthorize]` is unchanged. `AgentTokenGrant` is proof-of-possession for software agent keys, not a human chat credential.
- Answers render through the chat library. Approval is an inline card. No reasoning disclosure, feedback buttons, `@` tokens, or privacy notice.

## Decisions

### 1. Docked, non-modal side panel — adopt

Replace the Ask modal with a right dock opened from a top-bar **Ask AI** button and from Ctrl-K Ask. Ctrl-K closes the palette, then opens the same panel. Width is 450px when the viewport is at least 880px (the observed 450px-at-1280px width). Below 880px, and when the header expand control is on, the panel is `position: fixed` and covers the viewport. No scrim. The page reflows beside the dock.

Panel open, expand, edit mode, and the conversation credential live on `AgentSurfaceState`. `TimeWarpPage` remounts, so component fields would not survive navigation. While the panel is closed the shell stays `display: contents`, so the closed app bar matches the first-paint measurements. `TimeWarpFocusedPage` does not get the button.

The chat library's page is `height: 100vh`. The dock overrides that with a max height of `min(28rem, 60dvh)` so the header, transcript, and footer stay inside the panel.

### 2. Page-scoped tools and context — adopt (already shipped); one list for both drivers

271 already derives tools from the route. This task adds the missing parity piece: both drivers expose the same `page_context` tool (same name, description, and empty-object schema) after the catalog tools. The in-app function is a hand-built `AIFunction`, not `AIFunctionFactory`. The WebMCP descriptor carries `RequiresApproval`, which the browser script ignores. Invoke recomputes the bit from the current edit mode.

There is no context chip. `@` tokens and `page_context` read the same page facts.

A test crosses every `PageAgentScope` route, routes with no catalog tools, `/Feedback/item`, and `/FeedbackExtra` (must not inherit `/Feedback`), four principals, and both edit modes. Any difference in name, description, schema, approval bit, or order fails.

### 3. Ask before editing, with per-conversation auto-approve — adopt

Default is `AskBeforeEditing`. `AutomaticallyEdit` is stored on the conversation and cleared by New conversation. `CatalogAgentApproval.RequiresApproval(entry, mode)` is false for every tool in automatic mode. The read-only allow-list never prompts in either mode. `SelectAsync`, `FindOfferedAsync`, and `DescribeAsync` take the mode. The dispatcher and the in-app function read `AgentSurfaceState.EditMode` at invoke time. The panel rebuilds its tools when the mode or the conversation generation changes.

### 4. Per-conversation credential — adapt

Do not mint a bearer the model holds, and do not add a second authentication scheme. HTTP still uses the signed-in session. `[EndpointAuthorize]` is not bypassed.

`AgentConversationCredential` is an SPA value: id (`Guid.CreateVersion7`), principal, permission scopes, display name, and expiry. The issuer caps expiry at the earlier of `utcNow + lifetime` and the next UTC midnight. The default lifetime is 12 hours (`CredentialLifetimeMinutes` = 720). The display name defaults to "Ask AI conversation" and is truncated to 80 characters. A null credential is the legacy session path and is allowed.

`AgentConversationAuthority.Denial` checks, in order: expiry (`ExpiresAt <= now`), then principal mismatch only when both the user and the credential have a parseable `ClaimTypes.NameIdentifier` guid (a missing claim does not mismatch), then scope. Empty required permissions allow the call. Listing tools does not read the credential.

The chat approval wrapper runs before `InvokeCoreAsync`, so the credential check is inside `InvokeCoreAsync` after approval. WebMCP approves first when required, then refuses on denial before execute. `page_context` checks denial with an empty permission list. An expired credential in ask mode still prompts, then refuses. Automatic mode skips the prompt and refuses. The tool is still offered; denial is not implemented by refusing to wrap the function.

Messages:

- Expired: `This conversation's credential has expired.`
- Scope: `This conversation's credential does not include that action.`
- Principal: `This conversation's credential belongs to a different account.`

### 5. `@` resource references — adapt

Cloudflare tags workers, zones, and KV. This app's typed tokens come from the page-context JSON:

- `credentials[].id` plus nickname → `@credential:{id}`
- `profile.alias` → `@profile:{alias}`
- `siteSettings.version` → `@siteSettingsVersion:{n}`

Typing `@` opens that menu. Choosing an entry inserts the token into the chat input. There are no attachments.

### 6. Reasoning, tool steps, and feedback — adopt

Function-invocation blocks render inside a collapsible "See reasoning" section. The approval card stays. Each answer has copy, thumbs up, and thumbs down. Thumbs dispatch `FeedbackState.SubmitFeedback` with `FeedbackKind.Other`. Support is a native link to the configured URL and is omitted when that URL is empty. The relay is not token streaming. While `Status` is streaming, the panel shows "Thinking…".

### 7. Privacy notice and chat recording — adopt as configuration

`GetAgentChatConfiguration.Response` carries `RecordChats`, `PrivacyNotice`, `SupportUrl`, and `CredentialLifetimeMinutes`, copied from `XaiChatDefaults`. `RecordChats` defaults to false, so the notice stays hidden until a deployment turns recording on. Dismiss is per browser session on `AgentSurfaceState`. This task does not build a transcript store.

## Rejected

- A model-held API token or a new auth handler. The session already is the user's principal, and a second credential would race `[EndpointAuthorize]`.
- A standing agent key. The conversation value expires the same UTC day and is cleared by New conversation.
- An input context chip. The route and `page_context` already carry the page.
- A modal scrim. The dock leaves the page usable.
- Attachments. The reference input has none.
- True token streaming. The relay returns a completed response. The status line is the visible stage.
