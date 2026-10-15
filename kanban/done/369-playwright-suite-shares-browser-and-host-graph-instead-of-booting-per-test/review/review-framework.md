# Review framework

## Budget (by-diff)

- Lines changed: 350
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 369

**Date:** 2026-10-15
**Host task:** kanban/to-do/369-playwright-suite-shares-browser-and-host-graph-instead-of-booting-per-test/
**Diff scope:** branch task/369 commit 64abc83e5 vs b8cc9c0a1 (tests/container-apps/web/web-spa-playwright-tests/*)
**Plan / brief:** one Jaribu session-scoped Chromium (`PlaywrightBrowserSession`), fresh context per test; passkey InteractiveServer class shares its host per class; no C-share host (fixed in-proc ports, fresh-host assertions).
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
