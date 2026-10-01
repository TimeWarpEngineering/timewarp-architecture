# Round 1 — merged findings
**Date:** 2026-10-01
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: tools/dev-cli/endpoints/run-command.cs:29
- Description: Route description "(Development environment)" is not strictly true when a selected profile sets a different ASPNETCORE_ENVIRONMENT.
- Suggestion: Reword the description.
- Source: general
- Disposition notes: wontfix (orchestrator) — every declared profile sets Development, `dev run` itself still forces Development, and the precedence is documented in the Design region; a longer `--capabilities` label adds noise without changing behavior.

## Duplicates / conflicts

- None (single reviewer).
