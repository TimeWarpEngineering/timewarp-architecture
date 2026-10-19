# Round 1 — merged findings
**Date:** 2026-10-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: Directory.Packages.props:152
- Description: The TimeWarp.Nuru / DevCli 4.0.0-beta.2 rationale sits in the Microsoft.OpenApi
  comment block, not beside the Nuru pins at lines 12–13.
- Suggestion: Move the Nuru sentence to a comment directly above the Nuru pins. Remove it from the
  OpenApi block.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: Directory.Packages.props:165
- Description: The Microsoft.Extensions.AI.OpenAI comment still warns about "adapter 10.10.0"
  with OpenAI 2.14. The pin is now 10.11.0 and OpenAI 2.14.0 resolves.
- Suggestion: Drop the stale adapter version from the warning, or mark it as observed on 10.10.0
  and not re-checked on 10.11.0.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
