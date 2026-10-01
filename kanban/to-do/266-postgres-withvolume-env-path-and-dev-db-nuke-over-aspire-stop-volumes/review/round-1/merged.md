# Round 1 — merged findings
**Date:** 2026-10-01
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/db-nuke-command.cs
- Description: The sweep could remove volumes that the operator was never shown (the `--yes`
  path doesn't print, and the sweep re-lists after the stop).
- Suggestion: Print the volumes before the stop and sweep only the intersection with the initial list.
- Source: general
- Disposition notes: Fixed. The `--yes` path now prints `will delete: <name>` for every volume
  resolved before acting, and the sweep is limited to that list. The Design regions in both
  files are reconciled.

### M2 — Severity: nit — Status: fixed
- File: tools/dev-cli/services/db-nuke.cs
- Description: Sanitize used `char.IsNumber` (it accepts non-ASCII digits), unlike Aspire.
- Suggestion: Use `char.IsAsciiLetterOrDigit`.
- Source: general
- Disposition notes: Fixed.

## Duplicates / conflicts

- None.

## Fix verification

- dev-cli-tests 93/93, aspire-tests PostgresVolumeModel 4/4, `dev build` 0/0 on a fresh `bin/dev`
  (`ganda repo audit --fix --checks bin-dev`), `ganda repo audit` passes, and `dev db nuke`
  without `--yes` lists and exits 1.
