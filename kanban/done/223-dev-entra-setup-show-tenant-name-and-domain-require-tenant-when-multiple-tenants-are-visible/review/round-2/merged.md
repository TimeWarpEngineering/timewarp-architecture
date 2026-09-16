# Round 2 — merged findings
**Date:** 2026-09-16
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/entra-setup-command.cs:162
- Description: Setup Ambiguous now branches omitted vs provided `--tenant`. Provided values that match more than one tenant print `matched more than one tenant` (same as status) and show the narrowed candidate table.
- Suggestion: (applied in `e752204b`)
- Source: general
- Disposition notes: Re-verified in round 2. No reopen.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/services/entra-tenants.cs:39
- Description: Domain-only resolved tenants print `{domain} — {guid}` instead of `{guid} (name unavailable)`.
- Suggestion: (applied in `e752204b`)
- Source: general
- Disposition notes: Re-verified in round 2. No reopen.

## Duplicates / conflicts

- None. Prior M1/M2 carried forward; no new findings.
