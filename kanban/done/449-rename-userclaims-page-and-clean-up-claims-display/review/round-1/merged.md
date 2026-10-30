# Round 1 — merged findings
**Date:** 2026-10-30
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: kanban/in-progress/449-rename-userclaims-page-and-clean-up-claims-display/task.md:20
- Description: Checklist item "Verify admin + developer views" is ticked while Results disclose
  that no live browser check of the admin or developer view was done.
- Suggestion: Operator runs the "How to validate" steps in Results (`dev run`, sign in with
  `DeveloperClaimsRead`, open `/Developer/Claims`, repeat with `Authentication:UseMock=true`).
- Source: general
- Disposition notes: wontfix, decided by review oracle. Headless workers must not start the
  AppHost in a task worktree (shared user secrets), so neither the implementer nor the reviewer
  can perform the live check on this id. Results disclose the gap honestly and give the exact
  smoke steps. Automated coverage that does run here passed: page registry test (`ClaimsPage`
  registered), `dev build` 0/0, web-spa-integration-tests 569/569. No product change needed.

## Duplicates / conflicts

- Single reviewer; nothing to collapse.
