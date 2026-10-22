# Round 1 — merged findings
**Date:** 2026-10-22
**Sources:** general (one pass, split across the server and SPA/tests subagents; the orchestrator re-verified each finding)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 4 | 0 | 0 |
| suggestion | 4 | 0 | 0 |
| nit | 4 | 0 | 0 |

## Issues

Full description and evidence for each item are in `general.md`, Issue N = MN.

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs:89 (also the configuration and completion handlers, and `site-xai-api-key-application.cs:81`)
- Description: key resolution now reads Postgres on every Ask configuration load and completion. A missing table (42P01) or a database outage becomes a 500 instead of "not configured" (or 503 on Admin → AI).
- Suggestion: catch storage exceptions in `SiteXaiApiKey.ResolveApiKeyAsync` and return null. Map status, set, and clear to a 503, matching the site-settings precedent. Add a test.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/container-apps/web/features/agent-chat/xai-chat-contracts.cs:76; AiPage.razor:91-92; get-xai-api-key-status-handler-application.cs:63; xai-api-key-problems-application.cs:40
- Description: "Or open Admin → AI." is always appended. Without storage, Admin → AI tells the admin to open Admin → AI, which contradicts the Goal ("only shows how to set the host value"). `site-xai-api-key-tests.cs:134` asserts the wrong text.
- Suggestion: add the Admin sentence only when admin storage is available, and never on Admin → AI or the 503 detail. Flip the test.
- Source: general
- Disposition notes:

### M3 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:665
- Description: the Development `<pre>` setup command now ends with "Or open Admin → AI.", so the pasted command passes extra arguments to `dotnet user-secrets`. This regresses task 290's copy-paste command.
- Suggestion: keep the exact command alone in the `<pre>`, put the Admin sentence in its own field or paragraph, and put the Playwright assertions back.
- Source: general
- Disposition notes:

### M4 — Severity: bug — Status: open
- File: source/container-apps/web/features/agent-chat/site-xai-api-key-application.cs:90
- Description: a stored key that can't be unprotected shows "configured, ends in …xxxx" on Admin, while Ask is not configured. This contradicts the `IXaiApiKeyStatus.Configured` contract.
- Suggestion: base `Configured` on a trial resolve, or surface an unreadable / needs-replacement state. Add a test.
- Source: general
- Disposition notes:

### M5 — Severity: suggestion — Status: open
- File: projects/web-spa/features/admin/ai/xai-api-key-state/xai-api-key-state.set-xai-api-key.cs, xai-api-key-state.clear-xai-api-key.cs
- Description: an open tab's Ask readiness is not refreshed after a set or clear. Task 361 has a refresh-handler precedent for this.
- Suggestion: add a post-pipeline handler that dispatches `AgentSurfaceState.LoadChatConfiguration()`.
- Source: general
- Disposition notes:

### M6 — Severity: suggestion — Status: open
- File: site-xai-api-key-application.cs:138-148; ef-protected-secret-store-infrastructure.cs:76-87
- Description: two concurrent first sets race to a primary-key 500. The set/replace audit label comes from a separate read.
- Suggestion: have the store return inserted or replaced, and handle the unique violation or upsert.
- Source: general
- Disposition notes:

### M7 — Severity: suggestion — Status: open
- File: complete-agent-chat-handler-application.cs:58; xai-chat-upstream-server.cs:194
- Description: each turn makes two database reads and two unprotects.
- Suggestion: resolve once per turn.
- Source: general
- Disposition notes:

### M8 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-server-integration-tests/features/agent-chat/xai-api-key-admin-tests.cs:101-161
- Description: "Ask uses it" is not proven end to end. No test drives a completion through the real Postgres store and the upstream after an HTTP PUT.
- Suggestion: add a capturing `IXaiChatClientFactory` to `XaiApiKeyAdmin_` and assert the key a completion uses after the set and after the replace.
- Source: general
- Disposition notes:

### M9 — Severity: nit — Status: open
- File: source/container-apps/web/platform/postgres/migrations/20261021214830_AddProtectedSecrets.cs:9
- Description: the migration class is `_20261021214830_AddProtectedSecrets`, not the folder convention `AddProtectedSecrets`.
- Suggestion: rename both partials.
- Source: general
- Disposition notes:

### M10 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:108
- Description: `AutoComplete="off"` is ignored on password fields.
- Suggestion: use `new-password`.
- Source: general
- Disposition notes:

### M11 — Severity: nit — Status: open
- File: task.md `## Results`
- Description: the Results attribute the audit-line assertions to `XaiApiKeyAdmin_`. They live in `SiteXaiApiKey_Given_`.
- Suggestion: correct the Results text, or add the assertion.
- Source: general
- Disposition notes:

### M12 — Severity: nit — Status: open
- File: source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs:214
- Description: the old chat client is disposed on a key swap while a concurrent call may still be using it.
- Suggestion: don't dispose on swap, or defer the dispose.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- The SPA subagent's "admin page hint points at itself" and the server subagent's "status hint and 503 detail point back at Admin → AI" are the same root cause (`SetupHint` always appends `AdminAiSentence`). They are collapsed into M2 at the stronger severity (bug).
- The server subagent's "missing table / outage → 500" and "Admin status 500s the same way" are collapsed into M1.

## Evaluate

There are 12 open findings, so there is no disposition yet. A fix pass is needed on this task id. Round 2 re-verifies M1–M12 against the fix delta.
