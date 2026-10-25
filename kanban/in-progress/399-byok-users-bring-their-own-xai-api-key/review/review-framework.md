# Review framework

## Budget (by-diff)

- Lines changed: 793
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Round 2 scope

**Date:** 2026-10-26
**Host task:** kanban/in-progress/399-byok-users-bring-their-own-xai-api-key/
**Diff scope:** fix commit `fa3e2acfe` vs round-1 base `37ac90005` (13 product files, 1 test file)
**Plan / brief:** `review/round-1/merged.md` M1–M6, and task.md `## Goal (implementer restatement)`
**Effort:** 2, roster axes: general
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle under `ganda task work` headless; session id not exposed to the oracle

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
