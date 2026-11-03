# Review framework

## Budget (by-diff)

- Lines changed: 36442
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope and roster (round 1)

**Date:** 2026-11-04
**Host task:** kanban/in-progress/469-006-split-multi-type-test-files-and-refactor-test-files-over-500-lines/
**Diff scope:** branch `task/469-006-split-multi-type-test-files-and-refactor-test-file` vs `master` (merge base), commit 19e14bec3 (168 files, +19078 / -17364)
**Plan / brief:** split the 35 listed test files to 500 lines or fewer with no test or assertion lost (task.md Description, Requirements, Goal restatement)
**Effort:** 3; roster axis general, fanned out to three read-only general helpers by area (web-server integration, web-spa integration and playwright, analyzers / sourcegen / aspire / dev-cli / timewarp-testing / web-contracts), merged into one `general.md`
**Reviewer roster:** general
**Session IDs:** reviewer-claude (claude-opus-5-5) session 7ac650f1-9923-409a-ad64-528e17cfcca2

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
