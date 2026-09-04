# Round 3 — merged findings
**Date:** 2026-09-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: kanban/to-do/132-review-auth-authentication-authorization-feature-folder-naming-and-placement/disposition.md; inventory.md §9
- Description: 118 map overstated current sharing (api identity-host treated as future; catalogs claimed already referenced by both families).
- Suggestion: Present-tense taxonomy + Q6; document `api/platform/identity-host/`.
- Source: general (round 1)
- Disposition notes: Confirmed still fixed in round 3.

### M2 — Severity: bug — Status: fixed
- File: kanban/to-do/132-review-auth-authentication-authorization-feature-folder-naming-and-placement/inventory.md; disposition.md
- Description: M1 fix overstated duplicated `AgentTokenDefaults` as byte-identical; policy-name constants already differ.
- Suggestion: Token claim-type strings must stay aligned; policy names already differ; do not tell 118 the classes are identical.
- Source: general (round 2)
- Disposition notes: Confirmed still fixed in round 3 — “byte-identical” gone from inventory.md and disposition.md.

## Duplicates / conflicts

- None. No new findings in round 3.
