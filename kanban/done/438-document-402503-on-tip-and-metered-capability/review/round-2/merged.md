# Round 2 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:85
- Description: The documented challenge `resource.url` was a fixed route, not the configured
  `TipOptions.Resource` / `MeteredCapabilityOptions.Resource`.
- Source: general (round 1)
- Disposition notes: Fixed in `0264769f3`. The transformer reads the options from
  `context.ApplicationServices`. A test overrides both values and asserts that the document
  repeats them.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs:240
- Description: The test stubs hard-coded the routes instead of using the contract
  `RouteTemplate` constants.
- Source: general (round 1)
- Disposition notes: Fixed in `0264769f3`. The stubs and asserted paths use the contract
  constants.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:36
- Description: The dead `?? "/api/tip"` fallback.
- Source: general (round 1)
- Disposition notes: Removed in `0264769f3` together with M1.

## New findings

None.

## Duplicates / conflicts

- None.
