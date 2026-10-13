# Round 2 — merged findings
**Date:** 2026-10-13
**Sources:** general (re-review of 657470909 + round-1 M1–M8)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 4 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 3 | 0 |

(Counts include carried M1–M8, all re-verified fixed, plus N1–N2.)

## Resolved prior

M1–M8: re-verified fixed against 657470909 (see round-2/general.md table). No regressions.

## Issues

### N1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/ask-tag.ts:234
- Description: Enter/Tab pass through whenever focus is in the Ask panel; a keyboard user starting Tag from the focused button would have Tab/Enter work the panel instead of the pick. The Playwright test blurred focus first, hiding this.
- Suggestion: Test Enter on Tag without blur; narrow the pass-through.
- Source: general
- Disposition notes: Fixed in d96dfd149 — PickSession blurs focus left inside the Ask panel at start; Playwright now starts pick with Enter on the focused Tag button and Tab-cycles with no blur (11/11 pass). Enter on a re-focused Cancel still cancels.

### N2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:51
- Description: Duplicate "closing the panel cancels pick mode" sentence; several rewritten Design lines far over width.
- Suggestion: Drop duplicate; rewrap.
- Source: general
- Disposition notes: Fixed in d96dfd149.

## Duplicates / conflicts

- None.
