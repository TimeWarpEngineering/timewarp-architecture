# Review framework

## Budget (by-diff)

- Lines changed: 523
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 354

**Date:** 2026-10-14
**Host task:** kanban/to-do/354-admin-data-grids-keep-the-primary-column-visible-at-narrow-widths-instead-of-collapsing-it/
**Diff scope:** commit 6252a4237 (task branch vs df92be97c). The by-diff count of 523 lines also includes the task's own kanban edits.
**Plan / brief:** DataGridFrame wrapper; Grid mode with minmax GridTemplateColumns for the feedback and principals grids; source-scan guard; tw-blazor item 7; Playwright narrow/Ask-open proof.
**Effort:** 2 (budget by diff)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
