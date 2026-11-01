# Round 4 — merged findings
**Date:** 2026-11-01
**Sources:** general

Round 4 re-reviews the fix pass on round 3 (`review/round-3/merged.md`, now frozen), delta `285351c94..3cd8eef26`. Round-2 and round-3 IDs carry forward. There are no new findings.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs
- Description (round 2): the on-Settings AddPasskey call expected `InvalidOperationException`, but Mediator 15 records the failure as an `ExceptionNotification` error bar.
- Source: general
- Disposition notes: Fixed in `285351c94` and verified in round 3 (578/578). Round 4 did not change it.

### M2 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/spa-mediator-send-analyzer.cs:22
- Description (round 2): the analyzer Design region and the test stub described the Mediator 14 `ISender`.
- Source: general
- Disposition notes: Fixed in `285351c94` and verified in round 3 (259/259). Round 4 did not change it.

### M3 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:18
- Description (round 3): the AddPasskey Design sentences sat in front of "That test drives the real path…", so "That test" read as AddPasskey.
- Source: general
- Disposition notes: Fixed in `3cd8eef26`. "That test…" now follows the feedback-receipt sentences (lines 18–19), and the AddPasskey sentences follow it (lines 20–22). The change is comments only. `dev build` gave 0 warnings and 0 errors.

## Duplicates / conflicts

- None (single reviewer).
