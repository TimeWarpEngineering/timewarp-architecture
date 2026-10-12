# Round 1 — merged findings
**Date:** 2026-10-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: tests/container-apps/web/web-spa-playwright-tests/screenshot-path-guard-tests.cs:50
- Description: Guard regex only matches literals beginning with `kanban`; `"../kanban/"` or interpolated forms are not caught.
- Suggestion: Widen to any path segment inside a literal.
- Source: general
- Disposition notes: wontfix (orchestrator). Both former offenders used `Path.Combine(root, "kanban")`, which the guard catches. Widening to any segment inside any literal raises false-positive risk on prose strings for little gain; the task's requirement (a guard that fails at the offending source line) is met.

## Duplicates / conflicts

- None.
