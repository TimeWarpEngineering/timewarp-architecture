# Review framework

## Budget (by-diff)

- Lines changed: 836
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 294

**Date:** 2026-10-10
**Host task:** kanban/to-do/294-audit-and-trim-agentsmd-to-a-lean-core/
**Diff scope:** branch `task/294-audit-and-trim-agentsmd-to-a-lean-core` vs `master` (17 files)
**Plan / brief:** trim AGENTS.md to a lean core (<8 KB), move situational content to skills/analyzer release notes, audit table + contradictions in `audit.md`
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle Claude Opus 5.5 (ganda task work review node); reviewer subagent `general`

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
