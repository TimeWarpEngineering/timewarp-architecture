# Review framework

## Budget (by-diff)

- Lines changed: 847
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-04
**Host task:** kanban/in-progress/465-fix-dark-theme-broken-feedback-01a12f40-fd5d-77ca-beff-b33d0d65c964/
**Diff scope:** branch `task/465-fix-dark-theme-broken-feedback-01a12f40-fd5d-77ca` vs merge-base with `master`
(product commit 6be9c12c1 plus kanban commits). Product paths: `source/container-apps/web/projects/web-spa/**`
(tokens.css, app.css, eight `*.razor.css`, TimeWarpPage.razor, ColorTokensSample.razor, new
`application-state.toggle-theme.cs`, BaseComponent accessors), `skills/tw-blazor-css-strategy/SKILL.md`,
`tests/container-apps/web/web-spa-playwright-tests/dark-theme-playwright-tests.cs`.
**Plan / brief:** `## Goal (implementer restatement)` in task.md: dark `--twe-*` overrides keyed on Fluent's
`body[data-theme="dark"]`, tokenize hard-coded light colors, theme button dispatches a State action that
switches Fluent's mode, light theme unchanged, Playwright proof.
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle under `ganda task work` (Claude Opus 5.5); the oracle's session id is not
exposed to it.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
