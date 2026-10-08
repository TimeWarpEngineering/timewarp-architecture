# Review framework

## Budget (by-diff)

- Lines changed: 703
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework

**Date:** 2026-10-08
**Host task:** kanban/to-do/288-commit-non-secret-deploy-parameters-in-apphost-appsettings-instead-of-user-secrets/
**Diff scope:** branch task/288 vs master (commit aa529c441)
**Plan / brief:** task.md Description / Requirements (commit three app-identity deploy parameters in AppHost appsettings via derived `appNameKebab` template symbol; dev deploy resolution order; skill + smoke)
**Effort:** 2 (by-diff budget) — roster axes: general
**Reviewer roster:** general (Claude subagent, read-only)
**Session IDs:** review oracle (Claude Opus 5.5); general reviewer subagent ae1c94e3f85ac4543

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Prior rounds are immutable; new work goes in `round-(N+1)/`
