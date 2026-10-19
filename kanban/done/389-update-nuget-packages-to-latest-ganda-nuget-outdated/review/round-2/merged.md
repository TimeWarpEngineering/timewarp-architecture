# Round 2 — merged findings
**Date:** 2026-10-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: Directory.Packages.props:12
- Description: The Nuru 4.0.0-beta.2 rationale sat in the Microsoft.OpenApi comment block.
- Suggestion: Move it above the Nuru pins.
- Source: general (round 1)
- Disposition notes: Fixed in a1c69f3fb. The comment is directly above `TimeWarp.Nuru` and
  `TimeWarp.Nuru.DevCli`, and the OpenApi block no longer mentions Nuru.

### M2 — Severity: nit — Status: fixed
- File: Directory.Packages.props:165
- Description: The AI.OpenAI comment warned about adapter 10.10.0 while the pin is 10.11.0.
- Suggestion: Mark the warning as observed on 10.10.0 and not re-checked on 10.11.0.
- Source: general (round 1)
- Disposition notes: Fixed in a1c69f3fb with that wording. The Responses API guidance stays.

No new findings in round 2.

## Duplicates / conflicts

- None (single reviewer).
