# Round 2 — merged findings
**Date:** 2026-10-09
**Sources:** general (re-review)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- Disposition notes: `HttpAppBaseUrlAccessor` resolves the base URL in this order: `Mail:PublicBaseUrl`, then the forwarded proto and host (http or https only, first value, port kept), then the request. It returns null if that yields no absolute URL. The trust argument is in the Design region. Covered by 7 tests in `web-server-integration-tests/features/mail/http-app-base-url-accessor-tests.cs`.

### M2 — Severity: bug — Status: fixed
- Disposition notes: Mail is sent on a best-effort basis after the commit, using `CancellationToken.None`. A failure is logged as a warning and returns `EmailCopySent=false`. Test: `FailingMailSend_Should_StillReturnTheReceipt`.

### M3 — Severity: bug — Status: fixed
- Disposition notes: `FormatCompleted` is removed. `WebMcp_Approved_Submit_Feedback_Returns_The_Real_Receipt` drives `InvokeTool`, approval, and the ActionSet handler to `AgentCallOutcome`, and checks for `result.feedbackItemId` and `result.permalink` in the receipt. The API service is scripted in-process because the shared Aspire SPA host has no web-server API client.

### M4 — Severity: suggestion — Status: fixed
- Disposition notes: `DevelopmentEmailSender` is registered only when `Mail:Sender=Development`, which is set in `appsettings.Development.json`. Otherwise `UnconfiguredEmailSender` throws, and M2 turns that into `EmailCopySent=false`. The body is logged only at Debug. The "email copy available" UI flag is not yet tied to a real sender existing. That needs a capability member on `IEmailSender` and belongs with a real provider; the receipt stays honest without it.

### M5 — Severity: suggestion — Status: fixed
- Disposition notes: The submit handler puts the new item at the top of `Items` and removes any duplicate id. The page no longer re-lists. Covered by the real WebMCP submit test, which asserts `Items[0]`.

### M6 — Severity: suggestion — Status: fixed
- Disposition notes: `EmailMessage` rejects CR or LF in To and Subject (test `EmailMessage_Should_RejectLineBreaksInHeaders`). Results notes that the profile email is unverified and a rate limit is needed before a real provider goes live.

### M7 — Severity: nit — Status: fixed
- Disposition notes: The .eml now has `From: no-reply@localhost`. The Design regions and Results match.

### M8 — Severity: nit — Status: fixed
- Disposition notes: The class is renamed to `AddFeedbackItems`; the migration id is unchanged. The snapshot reformat is EF 11 tooling output. It is kept and flagged for the PR body.

## Duplicates / conflicts

- None.
