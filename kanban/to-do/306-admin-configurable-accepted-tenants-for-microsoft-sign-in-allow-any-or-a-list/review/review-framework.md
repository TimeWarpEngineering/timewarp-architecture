# Review framework

## Budget (by-diff)

- Lines changed: 1605
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 306

**Date:** 2026-10-21
**Host task:** kanban/to-do/306-admin-configurable-accepted-tenants-for-microsoft-sign-in-allow-any-or-a-list/
**Diff scope:** branch `task/306-admin-configurable-accepted-tenants-for-microsoft` vs `master` (505737cb0, fd9c3e2cd; 44 files)
**Plan / brief:** task.md `## Goal (implementer restatement)` + original Description/Requirements/Acceptance
**Effort:** 3 (by-diff budget); roster axes: general
**Reviewer roster:** general (review oracle, claude-opus-5-5, headless print mode)
**Session IDs:** review oracle session not exposed to the agent

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
