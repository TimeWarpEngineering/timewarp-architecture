# Review framework — task 296

**Date:** 2026-10-10
**Host task:** kanban/in-progress/296-ask-side-panel-dock-in-fluentlayout-aside-instead-of-collapsing-the-page/
**Diff scope:** branch `task/296-ask-side-panel-dock-in-fluentlayout-aside-instead` vs `master` (commit `baf2821ff`)
**Plan / brief:** task.md Requirements 1–5 — Ask rendered as the FluentLayout aside, flex dock removed, 880px breakdown + expanded full-screen rule, icon buttons, Playwright geometry tests.
**Effort:** 2 (Budget.ByDiff: 682 lines changed)
**Reviewer roster:** general
**Session IDs:** review oracle (Claude Opus 5.5, ganda task work headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
