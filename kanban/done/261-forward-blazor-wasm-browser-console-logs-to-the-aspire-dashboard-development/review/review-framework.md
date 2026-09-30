# Review framework

## Budget (by-diff)

- Lines changed: 663
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 261

**Date:** 2026-10-01
**Host task:** kanban/to-do/261-forward-blazor-wasm-browser-console-logs-to-the-aspire-dashboard-development/
**Diff scope:** branch `task/261-forward-blazor-wasm-browser-console-logs-to-the-as` vs `origin/master` (product files; kanban excluded)
**Plan / brief:** task.md: the fallback JS console hook, which POSTs to the `[ApiEndpoint]` `ForwardBrowserLogs`, which logs under the `Web.Spa.Browser` category (Development/Testing only)
**Effort:** 2 (by-diff budget), roster axes: general
**Reviewer roster:** general (Claude Opus subagent a62c6400b2388d870); orchestrator = review oracle (Claude Opus 5.5)
**Session IDs:** reviewer agent a62c6400b2388d870

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
