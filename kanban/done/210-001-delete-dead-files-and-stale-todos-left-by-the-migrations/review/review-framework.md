# Review framework — task 210-001

**Date:** 2026-09-12
**Host task:** kanban/in-progress/210-001-delete-dead-files-and-stale-todos-left-by-the-migrations/
**Diff scope:** branch `task/210-001-delete-dead-files-and-stale-todos-left-by-the-migr` vs `origin/master` (commit `c3b2beb0` — mechanical cleanup of parent 210 round-1 findings M11, M16, M31, M35, M37, M38, M39, M40, M41)
**Plan / brief:** Delete dead files and stale TODOs left by the migrations. One PR. Parent ledger: `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle 01a095cc-5427-7533-9567-6457da74d12a (2026-09-12)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
