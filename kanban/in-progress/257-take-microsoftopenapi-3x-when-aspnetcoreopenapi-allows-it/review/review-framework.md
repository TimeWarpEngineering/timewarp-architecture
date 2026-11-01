# Review framework

## Budget (by-diff)

- Lines changed: 22
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 257

**Date:** 2026-11-01
**Host task:** kanban/in-progress/257-take-microsoftopenapi-3x-when-aspnetcoreopenapi-allows-it/
**Diff scope:** uncommitted changes in the task worktree on `task/257-take-microsoftopenapi-3x-when-aspnetcoreopenapi-al` (8 product files; the branch has only kanban commits on top of master)
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: FastEndpoints 9.0.0-beta.6, Microsoft.AspNetCore.OpenApi 11.0.0-rc.3.26522.148, Microsoft.OpenApi 3.11.0, call-site fixes, OpenAPI/Scalar evidence, gates
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** reviewer-claude (claude-opus-5-5), headless ganda task-work review oracle; implementer-grok session 01a1960d-9a54-7072-bd5d-d3e0602b0acf

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
