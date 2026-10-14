# Review framework

## Budget (by-diff)

- Lines changed: 2532
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 300

**Date:** 2026-10-15
**Host task:** kanban/to-do/300-ask-give-the-ai-app-wide-pagenavigation-context-like-ctrl-k/
**Diff scope:** branch `task/300-ask-give-the-ai-app-wide-pagenavigation-context-li` vs `master` (commit 5df7913d2 over 71e879558)
**Plan / brief:** task.md Requirements — one registry-derived destination list for NavMenu, Ctrl-K, Ask and WebMCP; `site_map` + `navigate` tools; delete hand-written per-route agent map.
**Effort:** 3 (by-diff budget), roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
