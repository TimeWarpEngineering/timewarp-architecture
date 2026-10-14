# Round 1 — merged findings
**Date:** 2026-10-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: tests/container-apps/aspire/aspire-tests/logging-baseline-guard-tests.cs:137
- Description: sensitive-data guard is a file-level text match, not a call-site check.
- Suggestion: Roslyn analyzer for exactness.
- Source: general
- Disposition notes: wontfix (orchestrator) — no call site exists; spec asked for a cheap check;
  the guard trips on the first introduction and forces a reviewer to look. An analyzer is a
  candidate for the analyzer backlog, not this change.

## Duplicates / conflicts

- None (single reviewer).
