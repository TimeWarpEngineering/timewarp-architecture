# Review framework

## Budget (by-diff)

- Lines changed: 230
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 318

**Date:** 2026-10-12
**Host task:** kanban/to-do/318-generated-apps-ship-an-empty-kanban-board-skeleton/
**Diff scope:** branch task/318-… vs master (commit bc61ab468)
**Plan / brief:** pack an empty kanban skeleton (5 columns + .gitkeep, generic overview.md, task-template.md) into generated apps; template-smoke asserts it
**Effort:** 2 (by-diff budget); roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle (ganda task work, claude-opus-5-5)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
