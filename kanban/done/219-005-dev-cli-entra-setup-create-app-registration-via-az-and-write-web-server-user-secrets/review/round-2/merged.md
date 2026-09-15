# Round 2 — merged findings
**Date:** 2026-09-15
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/entra-setup-command.cs
- Description: Round 1 fail-open mint on `user-secrets list` failure. Re-verified: `TryDecideMintClientSecret` + abort path (`WriteFailure`, exit 1, no credential reset). `--new-secret` still mints without listing. Helper tests cover mint / skip / abort.
- Suggestion: none (fixed as recommended in round 1)
- Source: general
- Disposition notes: Verified on the post-fix delta. No new findings.

## Resolved prior

- M1 carried from round 1; status updated to `fixed`.

## Duplicates / conflicts

- None.
