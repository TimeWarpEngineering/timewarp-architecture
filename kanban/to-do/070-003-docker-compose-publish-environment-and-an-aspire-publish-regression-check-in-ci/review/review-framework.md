# Review framework

## Budget (by-diff)

- Lines changed: 694
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 070-003

**Date:** 2026-10-06
**Host task:** kanban/to-do/070-003-docker-compose-publish-environment-and-an-aspire-publish-regression-check-in-ci/
**Diff scope:** branch task/070-003-… commit acd2fc93b vs base 8e2424228 (HEAD~1..HEAD at review start)
**Plan / brief:** task.md Requirements 1–6 (Compose publish environment, production-safe output, postgres volume + migrations, CI-only artifacts, CI regression check)
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, headless ganda task work); general reviewer subagent a060f10106a6b303a

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
