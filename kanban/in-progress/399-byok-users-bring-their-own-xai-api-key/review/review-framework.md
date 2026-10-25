# Review framework

## Budget (by-diff)

- Lines changed: 3310
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 399

**Date:** 2026-10-26
**Host task:** kanban/in-progress/399-byok-users-bring-their-own-xai-api-key/
**Diff scope:** branch `task/399-byok-users-bring-their-own-xai-api-key` vs master merge-base `8b1230367` (commits c13056590, bf5489d04, 2dab9dc03; 62 product files)
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Decisions` (host key wins; Required / Allow / Off ladder; per-user row and Data Protection purpose; probe before save; merge deletes the row)
**Effort:** 3 (by-diff budget), roster axes: general
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle under `ganda task work` headless; session id not exposed to the oracle

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
