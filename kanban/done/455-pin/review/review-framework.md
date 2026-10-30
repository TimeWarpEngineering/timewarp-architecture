# Review framework

## Budget (by-diff)

- Lines changed: 46
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 455

**Date:** 2026-10-29
**Host task:** kanban/in-progress/455-pin/
**Diff scope:** branch `task/455-pin` vs `master` (`git diff master...HEAD`): `Directory.Packages.props` (1 line) and `kanban/in-progress/455-pin/task.md`
**Plan / brief:** change the central `Ardalis.GuardClauses` pin from `5.0.0` (a minimum) to the exact range `[5.0.0]`, the last MIT release; no other package moves
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), headless `ganda task work` 2026-10-29

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
