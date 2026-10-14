# Round 1 — merged findings
**Date:** 2026-10-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/ask-tag.ts:55
- Description: the single-slot `cancelledToken` forgets an earlier cancel. A late Start whose cancel
  was overwritten by a later cancel can arm and end a newer live pick.
- Suggestion: a refused-token set, bounded by deleting on refusal.
- Source: general
- Disposition notes: fixed in the review commit. `refused` set: CancelPick adds a token only when no
  session for it ever started (`active`/`latest` don't carry it), and Start refuses and deletes it.
  `dev build` 0/0; the Playwright sign-in test passed 3/3.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:49
- Description: the overlong Design comment line.
- Suggestion: rewrap.
- Source: general
- Disposition notes: rewrapped.

## Duplicates / conflicts

- None.
