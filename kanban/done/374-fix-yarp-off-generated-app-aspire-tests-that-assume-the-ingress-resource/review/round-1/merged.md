# Round 1 — merged findings
**Date:** 2026-10-16
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs:18, :67-69
- Description: the Design and SetupOnce comments describe the all-flags gate sequence. In
  flag-off variants some of those gates are compiled out.
- Suggestion: add a per-flag caveat next to those comments.
- Source: general
- Disposition notes: wontfix (orchestrator). The same Design region already has an explicit
  Task 374 paragraph that names each flag-guarded gate. The default (all-flags) app, which is the
  one most readers see, is described correctly. A comment-only edit doesn't justify another
  smoke and aspire-tests cycle. If it's worth doing, it can ride along with the task 375 nightly
  flag-off work.

## Duplicates / conflicts

- None (single reviewer).
