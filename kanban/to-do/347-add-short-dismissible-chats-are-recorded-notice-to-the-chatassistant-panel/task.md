# Add short dismissible "Chats are recorded" notice to the chat/assistant panel

## Description

Cloudflare's dashboard AI chat shows a dismissible banner above the input: "Chats are recorded to improve the service and are processed in accordance with our Privacy Policy." Steven wants the same idea in Architecture's chat UI, but **no legalese**. Plain copy: "Chats are recorded." Optionally link to a privacy page if the app has one (keep that optional).

Reference: `assets/cloudflare-chat-reference.png` (Cloudflare dashboard AI chat, Steven's screenshot 2026-10-13).

Existing chat surfaces to start from:
- `source/container-apps/web/projects/web-spa/features/chat/pages/ChatPage.razor`
- `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/` (`AgentAsk.razor`, `AskSuggestions.razor`, `AskAnswerBar.razor`, `AskStatusLine.razor`)

Keep it simple. No design work here (visual polish is 349).

## Requirements

- Small banner directly above the chat input
- Dismissible with an X; dismissal remembered per user/browser (e.g. local storage)
- Copy configurable per app (default "Chats are recorded."), optional privacy link URL
- Applies to whichever chat surfaces exist (ChatPage and/or Agent Ask)

## Checklist

- [x] Locate chat input surface(s)
- [x] Add notice component with configurable text and optional link
- [x] Persist dismissal per browser
- [x] Tests (renders, dismiss hides, stays dismissed on reload)

## Related

- 347 Chats-are-recorded notice
- 348 Starter prompts for primary use cases
- 349 Polished chat panel redesign (Opus 5.5)

## Session

- Created: 1440097 (2026-10-13)
- 2026-10-13: implemented under `ganda task work` (implement oracle).
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-13T15:28:52Z
- Review oracle: claude-opus-5-5, effort 2, general reviewer subagent a08274bbceb2e36f0 — accepted-exceptions, 2026-10-13
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-13T15:31:19Z

## Results

The Ask panel already had a session-only "Chats are recorded." notice (task 292), shown under the
header, gated on a hardcoded `RecordChats = false`. This task finishes it:

- **Banner above the input.** New `AskPrivacyBanner.razor` (+ scoped CSS) sits first in the Ask
  composer footer, directly above the tag chips and `MessageInput`. Plain text, optional
  **Privacy** link, X icon button (`aria-label="Dismiss notice"`). The old header block in
  `AgentAsk.razor` and its CSS were removed.
- **Configurable per app.** `XaiChatOptions` gains `RecordChats`, `PrivacyNotice`, `PrivacyUrl`
  (config `XAI:RecordChats`, `XAI:PrivacyNotice`, `XAI:PrivacyUrl`). The
  `GetAgentChatConfiguration` handler now reads them instead of constants. Defaults stay
  `XaiChatDefaults` (not recorded, "Chats are recorded.", no link). The response carries a new
  `PrivacyUrl`; the SPA keeps it only when it is a safe href (`AskSupportLink.NormalizeOptional`:
  app-relative or http/https; otherwise no link).
- **Dismissal remembered per browser.** `DismissPrivacyNotice` now also writes local storage key
  `twe.ask.privacy-notice-dismissed`; new `LoadPrivacyNoticeDismissal` action reads it back.
  `AgentAsk` dispatches it on first render, before the agent/footer is built, so a dismissed
  notice never flashes. Storage failures (`JSException`) fall back to a session-only dismissal.
- **ChatPage not changed.** `features/chat/pages/ChatPage.razor` is the SignalR hub demo. Its
  messages go through the hub and are not recorded, so "Chats are recorded." there would be false
  (the existing `AskPrivacyNotice` rule). Agent Ask is the only AI chat surface.

Tests: `tests/.../features/application/ask-privacy-banner-tests.cs` (new, Unit): renders text +
link + dismiss; renders nothing when not recorded; omits the link for blank/unsafe URLs; hides on
dismiss and stays dismissed after a "reload" (new container over the same dictionary-backed
`localStorage`, real Blazored service). `agent-ask-readiness-tests.cs` asserts `PrivacyUrl`
applied/reset. One parity test renamed for accuracy.

Gates run: `dev build` clean (0 warnings, 0 errors); `web-spa-integration-tests --filter-tag
Unit` 119/119 passed; `ganda repo audit` passes. The renamed Aspire-lane test
(`CatalogAgent_Should.Dismiss_Privacy_Notice_Hides_It_Across_New_Conversations`) was not
verified here: the worker has no Postgres for the Aspire host (Npgsql connect failure in
health checks). Its body only changed by name.

### How to validate

**Smoke:**

```bash
dev build
dotnet run --project tests/container-apps/web/web-spa-integration-tests -- --filter-class AskPrivacyBanner_
# Manual: set XAI__RecordChats=true and XAI__PrivacyUrl=/Privacy for web-server, `dev run`,
# sign in, open Ask AI, click the X on the notice, reload the page, reopen Ask AI.
```

**Expect:**

- Build has 0 warnings and 0 errors; `AskPrivacyBanner_` reports 4 passed.
- Manual: with `RecordChats=true` a small "Chats are recorded. Privacy" banner with an X sits
  directly above the Ask input; the link goes to `/Privacy`. After X it disappears and stays
  gone after a reload (local storage key `twe.ask.privacy-notice-dismissed` = `true`). With
  `RecordChats` unset (template default) no banner shows.

### Review disposition

- Rounds: 1 · effort 2 · roster: general
- Counts (final): bug 0 · suggestion 1 wontfix · nit 3 wontfix · open 0
- Disposition: **accepted-exceptions**. M1: the server mapping test needs the Aspire lane. M2: HtmlRenderer cannot click. M3: re-showing after a copy change was not requested. M4: default off is correct and XAI stays out of appsettings.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`
