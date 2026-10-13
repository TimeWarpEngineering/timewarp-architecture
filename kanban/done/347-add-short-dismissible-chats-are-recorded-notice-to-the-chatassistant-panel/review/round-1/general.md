# Round 1 — general
**Date:** 2026-10-13
**Scope reviewed:** git show HEAD (1bf0af06d)
## Summary
The change is sound. The banner moves into AskPrivacyBanner, and the dismissal is an action that writes local storage. The dismissal is restored in AgentAsk's first OnAfterRenderAsync, so prerender never touches JS interop. The footer (and so the banner) is only built after that await, so there is no dismissed-notice flash. PrivacyUrl is gated by the existing href scheme check (relative path, or http/https only; `//` and `/\` rejected). The reload test uses a second container over the same BrowserStorage and genuinely proves persistence. No stale references to the removed header notice or `twe-agent-ask__privacy` remain. Only test-coverage gaps and nits are left.
## Issues
### Issue 1 — Severity: suggestion
- File: source/container-apps/web/features/agent-chat/get-agent-chat-configuration/get-agent-chat-configuration-handler-application.cs
- Description: The new XAI:RecordChats / PrivacyNotice / PrivacyUrl to Response mapping (blank notice falls back to default, blank url becomes null, trim) has no test. The SPA tests script the Response, so a wrong config key or mapping would not fail any test. "Copy configurable per app" is therefore unproven end to end.
- Suggestion: Add a small handler or WebApplicationFactory test with XAI:* overrides asserting the Response fields, including the blank-notice and blank-url cases.
- Status: open
### Issue 2 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/ask-privacy-banner-tests.cs:87
- Description: The dismiss test sends DismissPrivacyNoticeActionSet.Action directly rather than clicking the X. The wiring from the X button's OnClick to DismissAsync is not exercised. The Playwright tests only assert count 0.
- Suggestion: Optional: add a Playwright or bUnit-style click check of AskPrivacyDismiss, or accept the current coverage.
- Status: open
### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent/ask-privacy-notice.cs:20
- Description: The storage key is a fixed boolean, so if an app later changes its PrivacyNotice copy or URL, users who dismissed the old notice never see the new one.
- Suggestion: Acceptable for the stated requirement. If wanted, store the notice text or a hash instead of `true`.
- Status: open
### Issue 4 — Severity: nit
- File: source/container-apps/web/features/agent-chat/xai-chat-contracts.cs
- Description: RecordChats defaults to false, so with default config the banner never renders. This is a documented prior design, but the task wording ("default 'Chats are recorded.'") could be read as shown by default. No sample XAI:RecordChats / PrivacyUrl in appsettings or a skill makes the switch hard to discover.
- Suggestion: Mention in task Results that the banner needs XAI:RecordChats=true, or add a commented sample config.
- Status: open
