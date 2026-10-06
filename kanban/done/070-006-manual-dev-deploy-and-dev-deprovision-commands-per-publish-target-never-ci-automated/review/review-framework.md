# Review framework

## Budget (by-diff)

- Lines changed: 1681
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 070-006

**Date:** 2026-10-07
**Host task:** kanban/to-do/070-006-manual-dev-deploy-and-dev-deprovision-commands-per-publish-target-never-ci-automated/
**Diff scope:** branch `task/070-006-…` vs `origin/master`, excluding content arriving via the merge of
`origin/task/070-005-…` (PR #438). 070-006 own commits: `2094f4636`, `98bdb0a3c` (plus the `tw-deploy`
skill sections they add).
**Plan / brief:** task.md Requirements — `dev deploy` / `dev deprovision` thin wrappers over
`aspire deploy` / `aspire destroy`, target-aware, preflight, confirmation, no-record guidance, never CI.
**Effort:** 3 — roster axis general, split into three read-only lenses
**Reviewer roster:** general, tests, plan_alignment
**Session IDs:** review oracle (claude-opus-5-5, headless ganda task work); reviewer subagents (sonnet)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
