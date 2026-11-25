# Round 2 — merged findings
**Date:** 2026-11-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: skills/tw-web-api-contracts/SKILL.md:469
- Description: Round 1: the canonical-examples line still called GetRoles "attribute auth + open-data" and GetRole "manual auth". Re-checked on the fix delta. The line now says `Search` is the only query parameter (GetRoles) and the route id is the whole request: `IAuthApiRequest`, no caller property, `I*Details` Response (GetRole). That matches `get-roles-contracts.cs` and `get-role-contracts.cs`.
- Suggestion: none remaining.
- Source: general
- Disposition notes: fixed in `22e842c7e`. Round 2 confirmed the living contracts.

## Issues

None.

## Duplicates / conflicts

- One reviewer. Nothing to collapse.
