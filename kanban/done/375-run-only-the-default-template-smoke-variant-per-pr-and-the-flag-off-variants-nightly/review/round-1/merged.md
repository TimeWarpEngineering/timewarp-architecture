# Round 1 — merged findings
**Date:** 2026-10-16
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: tools/dev-cli/endpoints/template-smoke-command.cs:177
- Description: default-only variant set was `SmokeMatrix[..1]` (positional coupling to entry order).
- Suggestion: select by meaning, not position.
- Source: general
- Disposition notes: fixed in the review pass — `SmokeMatrix.Where(variant => variant.ExtraArgs.Length == 0)`.
  Verified: `template-smoke --help` compiles and lists both options; `dev template-smoke` prints
  `Variants: SmokeDefault` and `Template smoke SUCCEEDED`.

## Duplicates / conflicts

- None (single reviewer).
