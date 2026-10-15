# Review framework

## Budget (by-diff)

- Lines changed: 562
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 370

**Date:** 2026-10-15
**Host task:** kanban/to-do/370-dev-test-runs-test-projects-in-parallel-with-per-project-in-process-port-bases/
**Diff scope:** branch `task/370-…` commit a13a95cb7 vs master (caf698bb2)
**Plan / brief:** parallel `dev test` runner — per-worker TIMEWARP_TEST_PORT_BASE, `--max-parallel` / `--max-integration`, buffered output + summary table, serial build phase then `--no-build --no-restore` tests; Playwright edit-mode click hardening.
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session id not exposed in print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
