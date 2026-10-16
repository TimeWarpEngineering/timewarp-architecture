# Round 1 — merged findings
**Date:** 2026-10-16
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/infrastructure/aspire-spa-test-application.cs:2
- Description: Purpose/Design regions, XML summary, and AddHttpClient comment still described an ingress-only base address.
- Suggestion: Reword to the flag-selected BaseUrlResourceName.
- Source: general
- Disposition notes: Fixed on this task by the review oracle (comment-only edit). The text now says "an Aspire resource HttpClient" and "an Aspire HTTP endpoint", and the AddHttpClient comment points at BaseUrlResourceName. Comment-only, with no `#if` change, so a re-review round was not needed. `ganda repo audit` and `dev build` were re-run after the fix.

## Duplicates / conflicts

- None.
