# Round 1 — merged findings
**Date:** 2026-10-15
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: tools/dev-cli/endpoints/deploy-migrate-command.cs:280
- Description: source ids from all sibling apps' migrations folders are compared with the single
  `web-migrations.sql`; a second postgres app would trip a false stale.
- Suggestion: pair each script with its app's folder.
- Source: general
- Disposition notes: wontfix (orchestrator). The template ships exactly one postgres app, and the
  verb itself applies only the one hard-coded script (`DeployOperate.MigrationScript`), so the
  whole verb, not just the check, is single-app today. Pairing scripts and folders belongs to
  whichever change makes migrate multi-database; doing it now would invent an app↔resource
  naming convention that nothing else follows.

## Duplicates / conflicts

- None (single reviewer).
