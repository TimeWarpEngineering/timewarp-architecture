# Round 5 — merged findings
**Date:** 2026-11-01
**Sources:** general

Round 5 reviews the CI fix for PR #620, commit `278424009` (delta `3cd8eef26..0151ed962`), which follows the `TimeWarp.State.DevTools.Hosting` `12.0.0-beta.16` resource name. Round-4 IDs carry forward (`review/round-4/merged.md` is now frozen). One new finding, M4.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 1 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs
- Description (round 2): AddPasskey on Settings under Mediator 15.
- Source: general
- Disposition notes: fixed in `285351c94`. Round 5 did not change it.

### M2 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/spa-mediator-send-analyzer.cs:22
- Description (round 2): SPA Send analyzer text described Mediator 14.
- Source: general
- Disposition notes: fixed in `285351c94`. Round 5 did not change it.

### M3 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:18
- Description (round 3): Design-region antecedent for "That test".
- Source: general
- Disposition notes: fixed in `3cd8eef26`. Round 5 did not change it.

### M4 — Severity: nit — Status: open
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:67; tests/container-apps/aspire/aspire-tests/state-dev-tools-model-tests.cs:13
- Description: both Design regions now record package version history (beta.15 named it `timewarp-state-devtools`; beta.16 names it `timewarp-devtools`). `tw-agent-context-regions` asks for timeless Design text, and both files ship in every generated app. The version numbers go stale at the next State bump.
- Suggestion: state the current fact only. The resource is `timewarp-devtools`, the package default, and the dashboard link is "TimeWarp DevTools". Drop the beta.15/beta.16 parentheticals. Change comments only.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
