# Round 1 — merged findings
**Date:** 2026-11-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: kanban/task-template.md:10
- Description: the shipped task template shows `## Propagation` to instance authors. The field
  applies only to tasks in this template, and the template line does not say so. The linked
  readme does.
- Suggestion: optionally scope the instruction line to template-repo tasks.
- Source: general
- Disposition notes: wontfix, decided by the review orchestrator. The acceptance criteria require
  the field in the packed template and the linked readme. The readme, one click away, already
  scopes the field to tasks in this template. A filled value on an instance task does no harm,
  because `ganda kanban upstream status` reads the upstream board. Not worth a fix pass.

## Duplicates / conflicts

- None (single reviewer).
