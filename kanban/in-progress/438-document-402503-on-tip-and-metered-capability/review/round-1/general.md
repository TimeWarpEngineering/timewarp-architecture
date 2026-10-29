# Round 1 — general
**Date:** 2026-10-29
**Scope reviewed:** `ce32e80af..HEAD` product paths: `open-api-payment-responses-server.cs`,
web-server `program.cs`, `CommonServerModule.AddOpenApi` overload, foundation global usings,
`llms.txt`, `auth.md`, `open-api-payment-responses-tests.cs`.

## Summary

The change adds a host callback to `CommonServerModule.AddOpenApi`. The web host uses it to
register document and operation transformers that add 402 (problem+json plus a required
`PAYMENT-REQUIRED` header, base64 x402 v2 JSON) and 503 (problem+json with `error`, `payment`,
`ok`) to the tip and metered operations. The metered operation also gets a `demo:invoke` note and
a debit note on 200. I checked the documented shapes against the handlers
(`submit-tip-handler-application.cs`, `invoke-metered-capability-handler-application.cs`), the
x402 payload (`payment-required-payload.cs`, `payment-accept.cs`), and the problem writer
(`base-fast-endpoint.cs`, which writes `application/problem+json`). They match, including `tip`
on tip only and `reason` on rejection. The implementation matches both the task and the
implementer restatement. `dotnet test web-server-integration-tests.csproj -- --filter-class
OpenApiPaymentResponses_` gives 5 passed. Risk is low. There are two maintainability gaps and one
nit, all about values kept in agreement by hand.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:36
- Description: The document states "Challenge resource.url is /api/tip" (and
  `/api/demo/metered-capability`) as a fact. The value actually sent comes from
  `TipOptions.Resource` (`tip-options-application.cs:39`) and `MeteredCapabilityOptions.Resource`
  (`metered-capability-options-application.cs:36`). Both are bindable configuration that defaults to
  the same literal. A deployment that overrides `Resource` gets an OpenAPI document that
  contradicts its own challenge. The transformer derives the path from `CanonicalTipPath` and the
  route template instead. That is a third copy of the value, kept equal by hand (AGENTS.md: "When
  two things must agree, generate one from the other or add a build check").
- Suggestion: Read the configured `Resource` in the transformer (resolve
  `IOptions<TipOptions>` / `IOptions<MeteredCapabilityOptions>` from
  `context.ApplicationServices`), or reword the text so it does not claim a fixed value.
  Add a test assertion that ties the documented resource to the options value.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs:191
- Description: The stub endpoints hard-code `"/api/tip"` and `"/api/demo/metered-capability"`.
  The transformer matches on `SubmitTip.Query.RouteTemplate`,
  `SubmitTipPost.Command.RouteTemplate`, and `InvokeMeteredCapability.Query.RouteTemplate`. If a
  contract route changes, the transformer follows it but the stubs keep the old path. The tests
  then fail for a stale stub, or, if both the stubs and the asserted paths are edited by hand,
  they pass without proving that the real contract route matches. Nothing builds the real
  web-server document, so this test is the only proof that the routes match.
- Suggestion: In the stubs, use `Get(SubmitTip.Query.RouteTemplate)`,
  `Post(SubmitTipPost.Command.RouteTemplate)`, and `Get(InvokeMeteredCapability.Query.RouteTemplate)`.
  Derive the asserted paths from the same constants (with a leading `/`).
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:36
- Description: `TipDiscoveryAlias.CanonicalTipPath.Value ?? "/api/tip"`. `CanonicalTipPath` is
  `new PathString("/api/tip")`, so `.Value` is never null. The fallback is a dead fourth copy of
  the literal.
- Suggestion: Drop the `?? "/api/tip"` fallback. This goes away anyway if Issue 1 reads the
  options value.
- Status: open
