# Round 1 — merged findings
**Date:** 2026-08-02
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: `tests/container-apps/web/web-spa-integration-tests/infrastructure/aspire-spa-test-application.cs`
- Description: Dead ScopedSender + Send overloads after SpaTestScope migration
- Suggestion: Remove dead API surface
- Source: general
- Disposition notes: Removed ScopedSender field and both Send methods (R1 fix)

### M2 — Severity: suggestion — Status: fixed
- File: `features/weather-forecast/weather-forecast-state-fetch-weather-forecasts-action-tests.cs`
- Description: Fully skipped class still paid full Aspire SetupOnce
- Suggestion: Drop host lifecycle while only fact is Skip
- Source: general
- Disposition notes: SetupOnce/CleanUpOnce/App/Spa removed; Design region notes re-add when un-skipping (R1 fix)

### M3 — Severity: nit — Status: fixed
- File: same weather fetch tests Skip message
- Description: Skip text still blamed toast ExceptionNotification after handler removal
- Suggestion: Point at remaining SPA→server fetch work (058)
- Source: general
- Disposition notes: Skip reason rewritten (R1 fix)

### M4 — Severity: nit — Status: wontfix
- File: `infrastructure/base-test.cs` SpaIntegrationHost.StartAsync
- Description: No DCP ingress reachability poll after Healthy (aspire-tests has it)
- Suggestion: Share poll if EOF flakes appear
- Source: general
- Disposition notes: Pre-migration SpaTestConvention also stopped at Healthy; current suite green; only wire-hitting facts care and weather is quarantined. Adopt if CI shows premature connection EOF. Decided by orchestrator.

## Duplicates / conflicts

- None
