# Review framework

## Budget (by-diff)

- Lines changed: 746
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-28
**Host task:** kanban/in-progress/423-breadcrumb-shows-each-style-guide-part-and-gets-real-styling/
**Diff scope:** branch `task/423-breadcrumb-shows-each-style-guide-part-and-gets-re` vs merge base
`c223df1cc` (commits `c7f5f1a52`, `79fbff09f`). The branch is 2 kanban-only land commits behind master.
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Requirements`: location
breadcrumb from `PageRegistry.All`, native styled nav, history trail removed from `TimeWarpPage`.
**Effort:** 2 (by-diff budget). The roster axis is general only.
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle session not reported by the host

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
