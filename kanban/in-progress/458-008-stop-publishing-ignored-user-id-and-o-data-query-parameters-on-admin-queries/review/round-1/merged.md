# Round 1 — merged findings
**Date:** 2026-11-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: skills/tw-web-api-contracts/SKILL.md:469
- Description: The canonical-examples paragraph still points at `get-roles/get-roles-contracts.cs` as "attribute auth + open-data" and at `get-role/get-role-contracts.cs` as "manual auth". Those were the two forms this change removed. The sections above already say `[ApiRoute]` is the only generated request attribute and that a hosted request does not name its caller. An agent that starts from the canonical-examples line is sent looking for `[AuthApiRequest]` and `[OpenDataQueryParameters]`.
- Suggestion: Rewrite that parenthetical to the living shape: `GetRoles` declares `Search` only, and `GetRole` is the route id plus `IAuthApiRequest` with no caller property.
- Source: general
- Disposition notes: fixed in the fix pass. The canonical-examples parenthetical now reads `Search` is the only query parameter (GetRoles) and the route id is the whole request: `IAuthApiRequest`, no caller property, `I*Details` Response (GetRole).

## Duplicates / conflicts

- One reviewer. Nothing to collapse.
