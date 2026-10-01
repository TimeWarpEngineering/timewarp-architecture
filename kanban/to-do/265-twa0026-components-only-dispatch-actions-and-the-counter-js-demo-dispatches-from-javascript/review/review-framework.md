# Review framework

## Budget (by-diff)

- Lines changed: 1389
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 265

**Date:** 2026-10-01
**Host task:** kanban/to-do/265-twa0026-components-only-dispatch-actions-and-the-counter-js-demo-dispatches-from-javascript/
**Diff scope:** branch `task/265-twa0026-components-only-dispatch-actions-and-the-c` vs `master` (commit 219fe923; 26 files, +1346/−43)
**Plan / brief:** task.md Requirements A (TWA0026/TWA0027 analyzer, attributes, registration, tests) and B (apply to web-spa: Counter JS dispatch, LoginPage/RedirectToLogin/AuthenticationStateListener converted to actions, CommandPalette opt-out)
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general, tests, plan-alignment (parallel, read-only)
**Session IDs:** review oracle (Claude Opus 5.5, headless ganda task work)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
