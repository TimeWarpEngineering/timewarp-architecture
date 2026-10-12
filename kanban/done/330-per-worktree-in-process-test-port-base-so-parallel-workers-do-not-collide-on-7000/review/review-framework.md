# Review framework

## Budget (by-diff)

- Lines changed: 575
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 330

**Date:** 2026-10-12
**Host task:** kanban/to-do/330-per-worktree-in-process-test-port-base-so-parallel-workers-do-not-collide-on-7000/
**Diff scope:** branch task/330-per-worktree-in-process-test-port-base-so-parallel vs master (76f31b7c9)
**Plan / brief:** task.md Requirements — per-checkout derived in-proc test port base, override precedence, collision message, dev test export, template-smoke base, text updates, tests
**Effort:** 2 (general axis, deeper pass)
**Reviewer roster:** general
**Session IDs:** ganda task work review oracle (Claude Code, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
