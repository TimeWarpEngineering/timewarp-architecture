# Review framework — task 299

**Date:** 2026-10-10
**Host task:** kanban/to-do/299-fix-ask-page-context-reasoning-shows-dictionary-tostring-and-wrong-route-and-make-feedback-details-full-width/
**Diff scope:** branch `task/299-fix-ask-page-context-reasoning-shows-dictionary-to` vs `master` (commit 61105fe89)
**Plan / brief:** task.md Requirements 1-6: JSON tool arguments in the Ask panel, page_context reports the on-screen route with Feedback facts, Feedback Details full width.
**Effort:** 3 (by diff), general only
**Reviewer roster:** general
**Session IDs:** review oracle (Claude Code, claude-opus-5-5), ganda task work 2026-10-10

## Budget (by-diff)

- Lines changed: 1002
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
