# Round 1 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-spa-playwright-tests/agent-links-onboarding-playwright-tests.cs:92
- Description: The prompt hand-types `{origin}/llms.txt` and `{origin}/auth.md`, and no test checks that the server serves either one (AGENTS.md: two things that must agree need a check).
- Suggestion: In the Playwright onboarding test, GET both URLs the prompt names through `page.APIRequest` and require HTTP 200.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/agent-links/agent-link-onboarding-prompt.cs:114 (and Design region line 27)
- Description: "PowerShell, where curl is an alias for Invoke-WebRequest" is true only of Windows PowerShell 5.1. PowerShell 7 has no such alias. The real reason is that the blocks are bash.
- Suggestion: Say the blocks are bash and do not run in PowerShell, and scope the alias remark to Windows PowerShell. Update the matching test strings.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/agent-links/agent-link-onboarding-prompt-tests.cs:113
- Description: `ShouldContain("printf '%s\\n'")` already matches pre-existing lines, so it does not pin the `b64url_decode` trailing-newline change.
- Suggestion: Assert the full helper line `printf '%s\n' "$data$pad" | tr '_-' '/+' | openssl base64 -d -A`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer.
