# Round 1 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:36
- Description: The documented challenge `resource.url` is fixed to `CanonicalTipPath` and the
  route template. The challenge actually sends the configurable `TipOptions.Resource` /
  `MeteredCapabilityOptions.Resource`, so the document is wrong when an operator overrides it.
- Suggestion: Resolve the options in the transformer through `context.ApplicationServices` and
  document that value, or reword so the text claims no fixed value. Add a test that ties the two.
- Source: general (Issue 1)
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs:191
- Description: The test stubs hard-code the routes instead of using the contract
  `RouteTemplate` constants the transformer matches on, so the test does not prove that the real
  routes match.
- Suggestion: Build the stub routes and the asserted paths from `SubmitTip.Query.RouteTemplate`,
  `SubmitTipPost.Command.RouteTemplate`, and `InvokeMeteredCapability.Query.RouteTemplate`.
- Source: general (Issue 2)
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:36
- Description: The `?? "/api/tip"` fallback can never run, because `CanonicalTipPath` is a
  non-null `PathString`.
- Suggestion: Remove it. M1's fix removes it too.
- Source: general (Issue 3)
- Disposition notes:

## Duplicates / conflicts

- None. M3 shares a line with M1, but it is a separate cleanup.
