# Review framework

## Budget (by-diff)

- Lines changed: 51
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Framework — task 379-006

**Date:** 2026-10-22
**Host task:** kanban/to-do/379-006-upstream-avatar-initials-circle-from-crunchit-portal/
**Diff scope:** branch `task/379-006-upstream-avatar-initials-circle-from-crunchit-port` (9a5271bd0) vs `master` (f64b498d7), `source/` and `tests/`
**Plan / brief:** task.md Checklist plus `## Goal (implementer restatement)`: a native initials `Avatar`, the `AvatarInitials` helper, `InitialsAvatarSize`, a style-guide card, an overview row, helper tests, and render tests
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (Claude Opus 5.5, headless ganda task work); session id not exposed

### Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
