# Round 1 — merged findings
**Date:** 2026-10-13
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 3 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: source/container-apps/web/features/agent-chat/get-agent-chat-configuration/get-agent-chat-configuration-handler-application.cs
- Description: No test covers the server mapping of `XAI:RecordChats` / `PrivacyNotice` / `PrivacyUrl` into the response (blank fallback, null URL, trim).
- Suggestion: Add a handler or endpoint test.
- Source: general
- Disposition notes: wontfix (orchestrator). Handler tests for web-application run in the Aspire lane, which the worker cannot host (no Postgres). Mocking the principal accessor and upstream to unit-test three lines of option mapping goes against the repo's integration-first testing rule. The SPA side, including unsafe-URL rejection, is covered. Candidate for the Aspire lane when it next runs.

### M2 — Severity: nit — Status: wontfix
- File: tests/container-apps/web/web-spa-integration-tests/features/application/ask-privacy-banner-tests.cs:87
- Description: Dismiss test dispatches the action directly instead of clicking the X.
- Suggestion: Click the button.
- Source: general
- Disposition notes: wontfix (orchestrator). The Unit lane renders with HtmlRenderer, which has no click events. The X only dispatches the action (all-interactions-are-State-actions rule), and the test asserts it renders `data-qa="AskPrivacyDismiss"`. Click coverage belongs in Playwright.

### M3 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/application/agent/ask-privacy-notice.cs:20
- Description: The dismissal key is a plain flag, so a later change to the notice copy stays hidden for users who already dismissed it.
- Suggestion: Version the key or store a hash of the copy.
- Source: general
- Disposition notes: wontfix (orchestrator). The requirement is "dismissal remembered per browser". Showing the notice again after a copy change is a new behavior with no request behind it.

### M4 — Severity: nit — Status: wontfix
- File: source/container-apps/web/features/agent-chat/xai-chat-contracts.cs
- Description: `RecordChats` defaults to false and the key is not shown in sample appsettings, so it is hard to discover.
- Suggestion: Document the sample setting.
- Source: general
- Disposition notes: wontfix (orchestrator). Default off is correct, because the template does not record chats and the notice would be false. The XAI section deliberately stays out of appsettings (see the Design region in `xai-chat-options-application.cs`). The options XML docs and task Results document the keys.

## Duplicates / conflicts

- None (single reviewer).
