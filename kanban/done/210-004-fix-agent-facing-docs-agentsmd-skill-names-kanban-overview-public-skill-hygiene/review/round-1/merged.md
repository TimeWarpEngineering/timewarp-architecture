# Round 1 — merged findings
**Date:** 2026-09-12
**Sources:** general, orchestrator merge verification

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md:28-30`
- Description: This change marked M18/M19/M26/M29 fixed and applied a delta to the parent 210 counts table (`bug` 14→11 open / 1→4 fixed; `nit` 9→8 open / 0→1 fixed; `suggestion` left at 17). The table still does not match the issue headings in the same file. Heading recount: `bug` 14 open / 4 fixed; `suggestion` 15 open; `nit` 7 open / 1 fixed. Origin/master already had a stale table (`bug` 14/1 vs headings 17/1; `suggestion` 17 vs 15; `nit` 9 vs 8); the delta preserved that undercount. Task 210-004 Results repeats the wrong table numbers.
- Suggestion: Recount from headings and set the table to `bug` 14/4/0, `suggestion` 15/0/0, `nit` 7/1/0. Update 210-004 Results to match.
- Source: orchestrator merge verification (general reported 0 issues; independently recounted headings)
- Disposition notes: Fixed on 210-004. Parent counts table and this task's Results now match heading recount (`bug` 14/4, `suggestion` 15/0, `nit` 7/1).

## Duplicates / conflicts

- General round-1 found no issues. Merge verification independently recounted parent 210 heading statuses vs the counts table this change edited and raised M1.
