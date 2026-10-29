# Review framework

## Budget (by-diff)

- Lines changed: 791
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Review framework — task 452

**Date:** 2026-10-30
**Host task:** kanban/in-progress/452-all-user-visible-dates-render-in-iso-format-with-analyzer-guard/
**Diff scope:** commit fc8bcb504 (`feat(analyzers): render all dates as ISO 8601 with TWA0036 guard`) vs its parent, on branch task/452-all-user-visible-dates-render-in-iso-format-with-a
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Checklist`; sweep user-visible dates to ISO 8601 and add analyzer TWA0036 with Jaribu tests
**Effort:** 2 (host Budget.ByDiff)
**Reviewer roster:** general (read-only sub-agent, claude-sonnet-5-5) plus orchestrator verification (Claude Fable 5.1: build, analyzer tests, web-spa integration tests, greps)
**Session IDs:** review oracle (ganda task work, headless print mode) 2026-10-30; general reviewer sub-agent a12e5e15df9cffc89

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
