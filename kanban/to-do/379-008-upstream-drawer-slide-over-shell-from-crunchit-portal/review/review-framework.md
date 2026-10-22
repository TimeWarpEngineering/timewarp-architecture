# Review framework

## Budget (by-diff)

- Lines changed: 1109
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-23
**Host task:** kanban/to-do/379-008-upstream-drawer-slide-over-shell-from-crunchit-portal/
**Diff scope:** branch `task/379-008-upstream-drawer-slide-over-shell-from-crunchit-por` vs `master` (commit 81d32f50c; 13 files: Drawer.razor/.css, drawer-side.cs, drawer-size.cs, drawer.ts, drawer-js-module.cs, app.css, tokens.css, StyleGuidePage.razor, overview.md, drawer-render-tests.cs, drawer-playwright-tests.cs, task.md)
**Plan / brief:** task.md Goal (implementer restatement), Decision, and Checklist: a native modal `<dialog class="twe-drawer">` slide-over, with caller-owned `IsOpen` and `OnClose`, a style-guide card, render tests, and one Playwright focus test.
**Effort:** 3
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle session not exposed in print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
