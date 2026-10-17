# Review framework

## Budget (by-diff)

- Lines changed: 106
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-17
**Host task:** kanban/in-progress/371-find-why-the-ask-edit-mode-menu-closes-under-parallel-load-instead-of-retrying-the-click-in-the-test/
**Diff scope:** branch `task/371-find-why-the-ask-edit-mode-menu-closes-under-paral` (98a89424f) vs `master` merge-base 6f19ce597
**Plan / brief:** product fix — `tooltip-hint.ts` makes every `fluent-tooltip` a `popover="hint"` so a delayed tooltip show no longer light-dismisses an open FluentMenu; 370's reopen-and-retry removed from `ask-surface-playwright-tests.cs`, replaced by a Copy-tooltip-keeps-menu-open assert on the edit-mode and settings menus.
**Effort:** 2 (general only, per by-diff budget)
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle session not exposed in print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
