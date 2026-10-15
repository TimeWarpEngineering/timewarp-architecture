# Review framework

## Budget (by-diff)

- Lines changed: 529
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-15
**Host task:** kanban/to-do/372-dev-test-runs-apphost-booting-suites-one-at-a-time-while-other-projects-stay-parallel/
**Diff scope:** branch task/372 vs merge base c9694b38c (commits e30518e54, 7583c5397)
**Plan / brief:** exclusive AppHost lane in `dev test` (derived via csproj/ProjectReference walk),
derived port ring below 32768, IPv4+IPv6 port-free check, Postgres Testcontainers start retry + host-port probe.
**Effort:** 2 — roster: general
**Session IDs:** review oracle (claude, claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Prior rounds are immutable; new work goes in `round-(N+1)/`
