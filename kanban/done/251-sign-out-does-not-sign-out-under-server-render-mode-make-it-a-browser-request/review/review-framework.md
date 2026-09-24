# Review framework — task 251

**Date:** 2026-09-24
**Host task:** kanban/to-do/251-sign-out-does-not-sign-out-under-server-render-mode-make-it-a-browser-request/
**Diff scope:** branch task/251-sign-out-does-not-sign-out-under-server-render-mod vs master (commit 9c874020)
**Plan / brief:** task.md Requirements — browser-facing antiforgery sign-out endpoint reusing EndBrowserSession.Handler, SPA sign-out via browser form POST in every render mode, tests
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** ganda task work review oracle (headless Claude), 2026-09-24

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
