# Review framework

## Budget (by-diff)

- Lines changed: 186
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 411

**Date:** 2026-10-24
**Host task:** kanban/to-do/411-nav-menu-keep-external-with-the-top-links-pin-only-navplacementbottom-groups/
**Diff scope:** branch `task/411-nav-menu-keep-external-with-the-top-links-pin-only` vs `master` (commit 16ba27dfc)
**Plan / brief:** External back in the primary nav; Developer pages back to Top; the pinned nav renders only when there are Bottom groups; `.twe-nav-menu` is the single scroll container (each FluentNav is `flex: none` and `overflow: visible`); comments, skill, and tests updated.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, print mode, session not reported)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
