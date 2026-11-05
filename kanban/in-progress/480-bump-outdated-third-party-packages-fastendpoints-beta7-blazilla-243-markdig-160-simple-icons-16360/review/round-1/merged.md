# Round 1 — merged findings
**Date:** 2026-11-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: Directory.Packages.props:146
- Description: The pin comment credits the `Microsoft.AspNetCore.OpenApi` dependency to
  "FastEndpoints" (the pair). The real source is `FastEndpoints.OpenApi`'s net11.0 group. The
  wording comes from the beta.6 comment.
- Suggestion: Name `FastEndpoints.OpenApi` in the comment.
- Source: general
- Disposition notes: wontfix (review oracle, 2026-11-05). This wording predates the change. The
  two packages are pinned in lockstep on adjacent lines, and the version facts in the comment are
  correct. A fix-pass round on a comment-only nit costs more than it buys. Fold the correction into
  the next FastEndpoints bump.

## Duplicates / conflicts

- None (single reviewer).
