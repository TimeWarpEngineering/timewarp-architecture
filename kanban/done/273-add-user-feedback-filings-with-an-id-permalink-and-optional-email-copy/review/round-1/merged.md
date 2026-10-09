# Round 1 — merged findings
**Date:** 2026-10-09
**Sources:** general

The orchestrator re-checked every finding against the worktree. All eight are real. Full text is in `general.md`; G# maps to M#.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 3 | 0 | 0 |
| suggestion | 3 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/platform/mail/http-app-base-url-accessor-server.cs:29
- Description: The emailed absolute permalink uses the request's own scheme and Host. Behind YARP that is the internal web-server http address.
- Suggestion: Use `Mail:PublicBaseUrl` from config first, then the forwarded proto and host, then the request. Otherwise use the relative link.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/container-apps/web/features/feedback/submit-feedback/submit-feedback-handler-application.cs:103-118
- Description: The row is saved before mail is sent. A send failure returns 500 with no id, so the filer loses the receipt and a retry creates a duplicate.
- Suggestion: Send mail on a best-effort basis after the save. On failure, log it and return `EmailCopySent=false`. Add a test with a throwing sender.
- Source: general
- Disposition notes:

### M3 — Severity: bug — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:543; web-mcp-dispatcher.cs:65
- Description: The WEBMCP-PROOF receipt is built from a hand-made object through the test-only `FormatCompleted`. The real `AgentCallOutcome` → tool result path is never tested, and the real field is `feedbackItemId`.
- Suggestion: Remove `FormatCompleted`. Drive the real dispatcher or the agent function for a feedback action, then fix the proof line in Results.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: source/container-apps/web/platform/mail/mail-module-server.cs:20
- Description: The development sender is registered in every environment. In production it reports `EmailCopySent=true` and delivers nothing. It also logs the full body at Information.
- Suggestion: Register the development sender only when it is opted into. Otherwise register an unconfigured sender that fails, so M2 reports false. Do not log the body at Information.
- Source: general
- Disposition notes:

### M5 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/feedback/feedback-state.submit-feedback.cs:72
- Description: An agent submit does not refresh the "Your filings" list. Only the page re-lists.
- Suggestion: Update `Items` in the submit handler and remove the page re-list.
- Source: general
- Disposition notes:

### M6 — Severity: suggestion — Status: open
- File: source/container-apps/web/platform/mail/email-message-application.cs:14
- Description: CR/LF in To or Subject can inject headers into the .eml file. The profile email is unverified, which the real-provider notes should mention.
- Suggestion: Reject CR/LF in the constructor. Add the note to Results.
- Source: general
- Disposition notes:

### M7 — Severity: nit — Status: open
- File: source/container-apps/web/platform/mail/email-message-application.cs:6
- Description: The Design region and Results claim a fixed From line that the code does not write.
- Suggestion: Add the From line.
- Source: general
- Disposition notes:

### M8 — Severity: nit — Status: open
- File: source/container-apps/web/platform/postgres/migrations/20261009001245_AddFeedbackItems.cs:9
- Description: The migration class is `_20261009001245_AddFeedbackItems`, unlike its siblings. EF 11 tooling reformatted the snapshot.
- Suggestion: Rename the class. Mention the snapshot reformat in the PR.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None.
