# Review framework

## Budget (by-diff)

- Lines changed: 845
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 312

**Date:** 2026-10-12
**Host task:** kanban/to-do/312-web-server-httpclients-and-service-uris-use-aspire-service-discovery-instead-of-hand-read-https-env-vars/
**Diff scope:** branch `task/312-…` vs `master` (commit 64eb82d29)
**Plan / brief:** task.md Requirements + Results (https+http service discovery for web-server named clients; `/service-discovery` + `ServiceUriProvider` + `ServiceUriHelper` deleted; X-Forwarded-Host replaces X-TimeWarp-Circuit-Host on the self-call; http-only regression test)
**Effort:** 3 (by-diff budget) — roster: general, security (host-header trust change rides on the general axis)
**Reviewer roster:** general, security
**Session IDs:** review oracle (claude-opus-5-5, ganda task work headless); reviewer subagents (Agent tool)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
