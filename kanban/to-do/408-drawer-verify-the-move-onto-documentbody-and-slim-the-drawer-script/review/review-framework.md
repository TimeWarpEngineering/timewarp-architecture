# Review framework

## Budget (by-diff)

- Lines changed: 423
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 408

**Date:** 2026-10-23
**Host task:** kanban/to-do/408-drawer-verify-the-move-onto-documentbody-and-slim-the-drawer-script/
**Diff scope:** branch `task/408-drawer-verify-the-move-onto-documentbody-and-slim` vs `8151be94b` (commit `f5eccc471`): `Drawer.razor`, `source/features/drawer.ts`, `drawer-playwright-tests.cs`, plus `task.md`
**Plan / brief:** task.md `## Goal` and `## Goal (implementer restatement)`: probe the drawer without `placeOnBody`, remove the move if not needed, add a navigate-away Playwright check, slim `drawer.ts`, and record why each remaining piece exists
**Effort:** 2 (by-diff budget above)
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, headless ganda task work; session id not exposed)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
