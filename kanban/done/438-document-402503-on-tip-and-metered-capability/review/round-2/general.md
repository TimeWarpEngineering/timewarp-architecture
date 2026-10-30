# Round 2 — general
**Date:** 2026-10-29
**Scope reviewed:** fix delta `1e59e8258..0264769f3` (round-1 base.sha → fix commit), product
paths `source/container-apps/web/platform/payment/open-api-payment-responses-server.cs` and
`tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs`.

## Summary

The fix pass resolves all three round-1 findings. The transformer now reads
`IOptions<TipOptions>` and `IOptions<MeteredCapabilityOptions>` from
`context.ApplicationServices`. Those are the same options the handlers pass to
`PaymentChallengeBuilder` (`Path = options.Resource`, serialized as `resource.url`), so the
document matches what the challenge sends. Both options are registered in web-server with
`AddFluentValidatedOptions(...).ValidateOnStart()`, so `.Value` cannot first throw when the
document is built. `Resource` is only validated when `Enabled`, and its default is the route.
The test host overrides both `Resource` values with paths that are not the routes, and it
asserts that the 402 descriptions contain the configured values and not the routes. Stubs and
asserted paths come from the contract `RouteTemplate` constants. I ran
`--filter-class OpenApi` in the integration test project: 23 passed, 0 failed (this includes
the payment class and the new `ConfiguredResource_Should_DocumentTheOptionsValue`).

Re-verified prior findings:

- M1 (configured resource): fixed. `open-api-payment-responses-server.cs:85-89` passes the
  options value, and `:320-324` holds the readers.
- M2 (contract route templates): fixed. The stubs use `SubmitTip.Query.RouteTemplate`,
  `SubmitTipPost.Command.RouteTemplate`, and `InvokeMeteredCapability.Query.RouteTemplate`.
  Asserted paths come from the same constants through `OpenApiPath`.
- M3 (dead fallback): fixed. `TipResource` / `MeteredResource` statics were removed.

The Goal restatement now includes the round-1 acceptance text. It is consistent with the
original task, so this is not a misread.

One cosmetic observation, not raised as a finding: after the Design-region edit at
`open-api-payment-responses-server.cs:12-15`, the line wrap is uneven ("…says / so, so a 402
… not read / as a response…"). It does not affect behavior or policy.

## Issues

<!-- None. -->
