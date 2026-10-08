# Review framework

## Budget (by-diff)

- Lines changed: 2310
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 271

**Date:** 2026-10-09
**Host task:** kanban/to-do/271-net-11-agentic-ui-over-the-timewarpstate-action-catalog/
**Diff scope:** branch `task/271-net-11-agentic-ui-over-the-timewarpstate-action-ca` vs `master` (commit d2f3a442c; 40 files, +2301/-9)
**Plan / brief:** `design.md` + task.md Results — in-process catalog agent (Components.AI-shaped, any `IChatClient`) and WebMCP publisher over the same page-scoped catalog tools; Visibility/permission filtering; in-app approval for mutating actions.
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general, security, tests
**Session IDs:** review oracle (Claude Opus 5.5, headless ganda task work); reviewer subagents spawned in-session

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
