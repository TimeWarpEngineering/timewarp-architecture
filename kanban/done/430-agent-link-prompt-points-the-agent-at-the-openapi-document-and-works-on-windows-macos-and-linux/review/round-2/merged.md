# Round 2 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/agent-links-onboarding-playwright-tests.cs:98
- Description: No test checked that the server serves `/llms.txt` and `/auth.md`, which the prompt names.
- Source: general (round 1)
- Disposition notes: The test reads both URLs from the rendered prompt and requires HTTP 200 (`RequireHttp200Async`).

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/agent-links/agent-link-onboarding-prompt.cs:116
- Description: The prompt said the curl alias applied to PowerShell in general. Only Windows PowerShell has that alias.
- Source: general (round 1)
- Disposition notes: The prompt, Design region, and caption now give the bash reason and limit the alias to Windows PowerShell. Both tests assert the new wording.

### M3 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/agent-links/agent-link-onboarding-prompt-tests.cs:115
- Description: The `printf '%s\n'` fragment matched lines that already existed, so it did not pin the `b64url_decode` change.
- Source: general (round 1)
- Disposition notes: The test asserts the full helper line. `Build_Should` passed 1/1 when re-run in round 2.

## Duplicates / conflicts

- None. Single reviewer. No new findings.
