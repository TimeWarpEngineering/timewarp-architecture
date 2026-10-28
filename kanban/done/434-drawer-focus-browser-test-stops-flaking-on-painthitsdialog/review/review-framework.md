# Review framework

## Budget (by-diff)

- Lines changed: 222
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 434

**Date:** 2026-10-29
**Host task:** kanban/in-progress/434-drawer-focus-browser-test-stops-flaking-on-painthitsdialog/
**Diff scope:** branch `task/434-drawer-focus-browser-test-stops-flaking-on-painthi` vs `master` merge base.
The product change is commit b8186f383 (`tests/container-apps/web/web-spa-playwright-tests/drawer-playwright-tests.cs`).
The other changes are kanban moves and the task body.
**Plan / brief:** In `AssertModalInTheShellAsync`, finish the dialog's open slide before reading the box. Keep the same
paint sample and also require that it lies inside the settled box and the viewport. Record the cause in Results.
**Effort:** 2 (general axis only, per budget)
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle under `ganda task work` (headless print mode; session id not exposed to the oracle)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
