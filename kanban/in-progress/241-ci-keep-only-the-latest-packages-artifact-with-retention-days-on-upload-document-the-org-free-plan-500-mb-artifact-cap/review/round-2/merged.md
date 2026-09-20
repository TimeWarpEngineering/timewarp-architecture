# Round 2 — merged findings
**Date:** 2026-09-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: documentation/developer/guides/releasing.md:52
- Description: Expired-artifact subsection previously framed a merge `Packages-*` + `gh run rerun` as required for this repo’s cut. Verified post-fix `33b35648`: this repo packs on `release:published`; merge rerun would not produce nupkgs; `gh run rerun` is scoped to the org-wide locate-run path.
- Suggestion: (applied)
- Source: general
- Disposition notes: Re-verified in round 2. No new findings.

## Resolved prior

- M1 carried from round 1; status remains `fixed`.

## Duplicates / conflicts

- None.
