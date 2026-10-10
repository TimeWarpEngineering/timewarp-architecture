# Review framework

## Budget (by-diff)

- Lines changed: 237
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 302

**Date:** 2026-10-11
**Host task:** kanban/to-do/302-fix-feedback-details-textarea-still-narrow-and-resize-grip-not-working-fluenttextarea-shadow-root/
**Diff scope:** branch `task/302-fix-feedback-details-textarea-still-narrow-and-res` vs `master` (commit aac5be3db)
**Plan / brief:** task.md Description and Requirements — size the visible shadow `part=root` of FluentTextArea, make resize work, measure the shadow box in Playwright.
**Effort:** 2 (by-diff budget, 237 lines)
**Reviewer roster:** general
**Session IDs:** review oracle (Claude Opus 5.5, ganda task work)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
