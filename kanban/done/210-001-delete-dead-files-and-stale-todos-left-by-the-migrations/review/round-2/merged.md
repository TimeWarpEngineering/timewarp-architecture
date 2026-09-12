# Round 2 — merged findings
**Date:** 2026-09-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs:29-30
- Description: After M11 deleted `tests/foundation/foundation-domain-jaribu-tests/`, this co-located runfile's NoWarn rationale still cited `tests/foundation/foundation-domain-jaribu-tests/Directory.Build.props` as the Jaribu precedent. Comment-only (build/tests unaffected), but it was a dangling product-source path left by the delete.
- Suggestion: Retarget the citation to the live precedent — `tests/Directory.Build.props`.
- Source: general
- Disposition notes: Retargeted to `tests/Directory.Build.props`. Round-2 re-verified: `source/` and `tests/` have zero remaining `foundation-domain-jaribu-tests` hits.

## Duplicates / conflicts

- None. Prior M1 carried forward; no new findings.
