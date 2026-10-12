# Review framework

## Budget (by-diff)

- Lines changed: 645
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 327

**Date:** 2026-10-13
**Host task:** kanban/to-do/327-sortable-columns-on-the-admin-feedback-list-via-fluentdatagrid-with-server-side-sort/
**Diff scope:** branch task/327 commit 35ecb7a04 vs HEAD~1
**Plan / brief:** task.md Requirements (server-side sort contract, store, FluentDataGrid, tests, tw-blazor note)
**Effort:** 2 (by-diff budget); roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
