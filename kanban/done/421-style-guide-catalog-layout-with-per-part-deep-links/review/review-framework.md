# Review framework

## Budget (by-diff)

- Lines changed: 119
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Framework — task 421

**Date:** 2026-10-27
**Host task:** kanban/in-progress/421-style-guide-catalog-layout-with-per-part-deep-links/
**Diff scope:** branch `task/421-style-guide-catalog-layout-with-per-part-deep-link` at `964b41571` vs merge-base `e7f0fa292` (master)
**Plan / brief:** task.md Requirements, Acceptance criteria, and `## Goal (implementer restatement)`
**Effort:** 3 (by-diff budget), roster axis general only
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle session not reported by host

## Round 2 scope

**Diff scope:** fix delta `8e9eed258..0a49d867d` (commits `ad83f400b` and `0a49d867d`), which is the fix pass for round-1 M1, M2, and M3
**Effort:** 1 (by-diff budget, 119 lines), roster axis general only
**Reviewer roster:** general (review oracle, claude-opus-5-5)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
