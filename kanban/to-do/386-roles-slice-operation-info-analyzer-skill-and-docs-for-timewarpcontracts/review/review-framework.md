# Review framework — task 386

**Date:** 2026-10-20
**Host task:** kanban/to-do/386-roles-slice-operation-info-analyzer-skill-and-docs-for-timewarpcontracts/
**Diff scope:** branch `task/386-roles-slice-operation-info-analyzer-skill-and-docs` vs `master` (commits `4ca804989`, `a07d79ffd`)
**Plan / brief:** Adopt `TimeWarp.Contracts` in the roles slice: info analyzer TWA0035, `[Operation]` on the six roles requests, kind interfaces where the kind is clear, and the neutral-layer write-up in `tw-web-api-contracts`. CLI and MCP proof stays out of scope.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** Grok `01a157e6-d044-7442-b624-c74e144b8fff` (2026-10-20)

## Budget (by-diff)

- Lines changed: 883
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
