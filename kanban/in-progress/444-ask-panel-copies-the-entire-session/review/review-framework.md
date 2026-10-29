# Review framework

## Budget (by-diff)

- Lines changed: 185
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Round 1 framework

**Date:** 2026-10-29
**Host task:** kanban/in-progress/444-ask-panel-copies-the-entire-session/
**Diff scope:** branch task/444-ask-panel-copies-the-entire-session vs merge-base 7181632c9 (master), product paths only: `AskAnswerBar.razor`, `ask-surface-playwright-tests.cs`
**Plan / brief:** task.md Description, Checklist, and `## Goal (implementer restatement)`; implement commit 39eeac640
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle, Claude Fable 5.1 (claude-fable-5-1), headless `ganda task work` review node, 2026-10-29

### Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
