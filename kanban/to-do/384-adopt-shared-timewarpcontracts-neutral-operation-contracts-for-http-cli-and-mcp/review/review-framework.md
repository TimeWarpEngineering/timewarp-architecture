# Review framework

## Budget (by-diff)

- Lines changed: 880
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 384

**Date:** 2026-10-18
**Host task:** kanban/to-do/384-adopt-shared-timewarpcontracts-neutral-operation-contracts-for-http-cli-and-mcp/
**Diff scope:** branch `task/384-adopt-shared-timewarpcontracts-neutral-operation-c` vs `master` (9ebb71103..d1b2d60e9, one commit, 28 files, +862/−18)
**Plan / brief:** task.md `## Goal (implementer restatement)`, `## Scope of this task` (step 2 only), `## Home decision` (option b)
**Effort:** 3 (by-diff budget above); roster axis general only
**Reviewer roster:** general
**Session IDs:** review oracle claude-opus-5-5 (headless, session id not exposed); implementer implementer-grok (grok-4.7, session not reported)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
