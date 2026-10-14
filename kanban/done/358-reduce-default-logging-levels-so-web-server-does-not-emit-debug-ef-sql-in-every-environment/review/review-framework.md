# Review framework

## Budget (by-diff)

- Lines changed: 514
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 358

**Date:** 2026-10-14
**Host task:** kanban/to-do/358-reduce-default-logging-levels-so-web-server-does-not-emit-debug-ef-sql-in-every-environment/
**Diff scope:** branch `task/358-reduce-default-logging-levels-so-web-server-does-n` vs `master` (commit f26bbc363)
**Plan / brief:** Information baseline in every host's base appsettings.json, EF SQL at Warning, Development-only Debug for the app's own namespaces, dead Kubernetes_Docker files removed, guard + publish-suite assertions, Pkcs NU1902/3 boyscout pin, Playwright focus-race fix.
**Effort:** 2 (by-diff budget); roster axes: general
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** not reported by host

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
