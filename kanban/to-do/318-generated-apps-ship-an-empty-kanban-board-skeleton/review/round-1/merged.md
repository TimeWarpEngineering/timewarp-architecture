# Round 1 — merged findings
**Date:** 2026-10-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: kanban/task-template.md:49
- Description: The shipped Smoke example uses `./bin/dev run`, which generated apps don't have.
- Suggestion: Use a generic AppHost `dotnet run --project` example.
- Source: general
- Disposition notes: Fixed on this task. The example now reads `dotnet run --project source/container-apps/aspire/projects/aspire-app-host`.

## Duplicates / conflicts

- None.
