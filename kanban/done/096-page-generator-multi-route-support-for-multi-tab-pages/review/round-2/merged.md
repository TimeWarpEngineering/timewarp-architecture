# Round 2 — merged findings (re-verify)
**Date:** 2026-10-03
**Sources:** review oracle re-verify of the fix delta

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 2 | 2 |

## Resolved prior

- M1 fixed: the Design region documents the token forms covered by TWE010.
- M2 fixed: `ParseRoute(additional, parameters, …)`, and the `knownParameters` rename is applied at every call site.
- M5, M6, M7 fixed: the sourcegenerator-tests suite passes 103/103 (was 99), and `./bin/dev build` reports 0 warnings, 0 errors.
- M3, M4 wontfix: unchanged; rationale in round-1/merged.md.

## New findings

- None. Fix delta: generator rename and alias-agreement scope, docs wording, tests, and the test-helper `.Distinct()`.
