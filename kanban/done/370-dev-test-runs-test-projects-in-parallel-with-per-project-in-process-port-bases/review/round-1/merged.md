# Round 1 — merged findings
**Date:** 2026-10-15
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/test-command.cs:190
- Description: Semaphore-based scheduling starves capped (integration) projects behind every queued
  fast project once the integration cap is reached; the documented start order does not hold.
- Suggestion: single dispatcher loop picking the first pending project (in `TestParallelism.Order`)
  whose group cap has room whenever a worker / port base is free.
- Source: general
- Disposition notes: fixed on this task — `RunAllAsync` is now a single dispatcher loop (free port-base queue, per-group running counts, `Task.WhenAny`); semaphores and ConcurrentQueue removed. Verified: `dev test --no-build -j 4 --quiet` 23/23 in 163s, all integration projects started before any fast project.

## Duplicates / conflicts

- None (single reviewer).
