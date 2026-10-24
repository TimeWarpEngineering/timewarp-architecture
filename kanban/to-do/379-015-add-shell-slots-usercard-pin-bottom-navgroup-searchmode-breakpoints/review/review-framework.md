# Review framework

## Budget (by-diff)

- Lines changed: 555
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 379-015

**Date:** 2026-10-24
**Host task:** kanban/to-do/379-015-add-shell-slots-usercard-pin-bottom-navgroup-searchmode-breakpoints/
**Diff scope:** `82ef665fc..cb9a17ddd`, the task commit on top of the 379-014 merge. The
branch is 25 commits ahead of `origin/master` because it stacks 379-012 through 379-014. Those
commits were reviewed on their own ids and are out of scope here.
**Plan / brief:** `## Goal (implementer restatement)` in task.md. It covers UserCard and the
shell slot, NavPlacement, ShellSearchMode, and the rail and drawer breakpoints.
**Effort:** 3 (general axis only)
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle under ganda task work (headless print mode; no session id reported)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
