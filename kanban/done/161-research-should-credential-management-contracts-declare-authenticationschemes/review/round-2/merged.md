# Round 2 — merged findings
**Date:** 2026-09-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: kanban/in-progress/161-research-should-credential-management-contracts-declare-authenticationschemes/task.md (coverage audit intro)
- Description: Round-1 coverage-audit heading attributed InvokeMeteredCapability anonymous 401 to `web-server-integration-tests`. That case is `Unauthorized_Given_No_Bearer` in `invoke-metered-capability-tests.cs`.
- Suggestion: Name the co-located runfile (or broaden the HostGraph label).
- Source: general (round 1); re-verified round 2
- Disposition notes: Intro now distinguishes the suite project from the co-located InvokeMetered anonymous test. Product code unchanged.

## Resolved prior

- M1 carried from round 1; status `fixed`. No new IDs.

## Duplicates / conflicts

- None.
