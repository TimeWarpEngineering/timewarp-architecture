# Round 2 — merged findings
**Date:** 2026-10-17
**Sources:** general (re-verify of the M1 fix delta: `command-palette.ts`, `ask-ai.ts`,
`focus-return-claim.ts`, new Playwright test)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- Re-verified: both orders (palette restores first, or Ask claims first) give Ask a guarded
  restore for a trigger target; non-trigger openers (AskAiButton) still focus directly; the
  header-button close test still passes. `dev test` 23/23 projects (web-spa-playwright-tests
  22/22, including the new search-field test).

## New findings

- None on the fix delta.
