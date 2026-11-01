# Round 6 — merged findings
**Date:** 2026-11-01
**Sources:** general

Round 6 reviews the M4 fix pass, commit `1ca6eae81` (delta `0151ed962..1ca6eae81`). Round-5 IDs carry forward, and `review/round-5/merged.md` is now frozen. This round raises no new findings.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs
- Description (round 2): AddPasskey on Settings under Mediator 15.
- Source: general
- Disposition notes: fixed in `285351c94`. Round 6 did not change it.

### M2 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/spa-mediator-send-analyzer.cs:22
- Description (round 2): the SPA Send analyzer text described Mediator 14.
- Source: general
- Disposition notes: fixed in `285351c94`. Round 6 did not change it.

### M3 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:18
- Description (round 3): the antecedent of "That test" in the Design region.
- Source: general
- Disposition notes: fixed in `3cd8eef26`. Round 6 did not change it.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:66; tests/container-apps/aspire/aspire-tests/state-dev-tools-model-tests.cs:13
- Description (round 5): both Design regions recorded the beta.15 and beta.16 version history.
- Source: general
- Disposition notes: fixed in `1ca6eae81`. Both regions now state that the resource is `timewarp-devtools`, the package default, and that the dashboard link is "TimeWarp DevTools". No version numbers remain, and the change touches comments only. `dev build` gave 0 warnings and 0 errors.

## Duplicates / conflicts

- None (single reviewer).

## Out-of-scope observation

- `source/container-apps/web/projects/web-spa/program.cs:36` still names `12.0.0-beta.15`. That line was written before this task, is outside this task's diff, and is not a finding. See `general.md`.
