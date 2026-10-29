# Document 402/503 on tip and metered-capability

## Description

Per `llms.txt`/`auth.md`, `GET|POST /api/tip` returns 402+PAYMENT-REQUIRED
when payment is enabled (503 when disabled), and
`GET /api/demo/metered-capability` needs Bearer `demo:invoke`, returning 402
or 200-with-debit (503 when disabled). The OpenAPI documents only 200 for
both, with no scope hint.

## Goal (implementer restatement)

The OpenAPI document for this host should tell the truth that `llms.txt` and `auth.md` already tell. `GET` and `POST /api/tip` can answer 402 with an x402 `PAYMENT-REQUIRED` challenge when payment is enabled, and 503 when payment is disabled or misconfigured. `GET /api/demo/metered-capability` is the same pair of payment outcomes, and the operation must say the caller needs bearer scope `demo:invoke` (a 200 on that route is a ledger debit). Free and discovery routes, including fetching the OpenAPI document itself, still never return 402. The bare `/api` path stays a tip-jar alias, not a separate operation and not a free route.

Round-1 review acceptance: the documented challenge `resource.url` is the configured `TipOptions.Resource` or `MeteredCapabilityOptions.Resource`, resolved from the host services when the document is built, so an operator override is what the document states. The test host sets those values to something other than the HTTP route and asserts the document matches the options. Stub routes and asserted operation paths come from `SubmitTip.Query.RouteTemplate`, `SubmitTipPost.Command.RouteTemplate`, and `InvokeMeteredCapability.Query.RouteTemplate`. The dead `?? "/api/tip"` fallback is gone.

## Checklist

- [x] Add 402 (x402 challenge shape) and 503 responses to /api/tip (GET+POST)
- [x] Add 402/503 + demo:invoke scope note to /api/demo/metered-capability
- [x] Keep free/discovery never-402 statement accurate
- [x] Document challenge resource.url from TipOptions.Resource and MeteredCapabilityOptions.Resource
- [x] Drive the payment OpenAPI test routes from the contract RouteTemplate constants
- [x] Remove the dead CanonicalTipPath `?? "/api/tip"` fallback

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)
- Implementation: task-work implement oracle (2026-10-29)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a18528-d963-7fb2-9d37-b4a636af295b, max-turns 500, Done — 2026-10-29T01:10:04Z
- Review oracle round 1: general reviewer (claude-opus-5-5, effort 2), 2026-10-29. Findings M1–M3 open, so a fix pass is requested.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session dae4a414-145c-40a1-8f22-c2a65b22724c, max-turns 120 — 2026-10-29T01:13:11Z
- Implement-fix-2: addressed M1–M3 (2026-10-29). The document reads the configured resource from the host options, and the payment OpenAPI test uses the contract route templates.
- Review oracle round 2: general reviewer (claude-opus-5-5, effort 2, headless), 2026-10-29. M1–M3 are fixed and there are no new findings. Disposition clean.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session 01a18528-d963-7fb2-9d37-b4a636af295b, max-turns 500, Done — 2026-10-29T06:08:45Z

## Results

The web OpenAPI document now lists HTTP 402 and 503 on `GET` and `POST /api/tip` and on `GET /api/demo/metered-capability`. The 402 body is `application/problem+json`. The `PAYMENT-REQUIRED` header is a base64 x402 v2 object (`x402Version`, `accepts` with `scheme`, `network`, `payTo`, `price`, and `resource.url`). The documented `resource.url` is `TipOptions.Resource` on the tip operations and `MeteredCapabilityOptions.Resource` on the metered operation, read from the host services when the document is built. 503 is `application/problem+json` with `error`, `payment`, and `ok`, and it has no `PAYMENT-REQUIRED` header. The metered operation requires bearer scope `demo:invoke` and its 200 description says the call debits the ledger (`balanceAfter`). The document description states that free and discovery operations never return 402, and that bare `/api` is a tip-jar alias rather than its own operation. `llms.txt` and `auth.md` say the catalog lists those codes on the paid operations only, and that fetching `/openapi/v1.json` never returns 402.

### How to validate

Smoke: from `tests/container-apps/web/web-server-integration-tests` (its `global.json` selects the Microsoft.Testing.Platform runner), `dotnet test web-server-integration-tests.csproj -- --filter-class OpenApiPaymentResponses_`.

Expect: 6 passed. `GET` and `POST /api/tip` are anonymous and document 402 (header `PAYMENT-REQUIRED` required, description and schema include `x402Version`, `accepts`, `payTo`, and `base64`) and 503 (no `PAYMENT-REQUIRED`). `GET /api/demo/metered-capability` documents the same pair, `security` scope `demo:invoke`, and a 200 description that contains `balanceAfter`. Those operation paths come from `SubmitTip.Query.RouteTemplate`, `SubmitTipPost.Command.RouteTemplate`, and `InvokeMeteredCapability.Query.RouteTemplate`. The test host sets `TipOptions.Resource` to `/pay/configured-tip` and `MeteredCapabilityOptions.Resource` to `/pay/configured-meter`, and the 402 descriptions contain those values and not the HTTP routes. `GET /api/hello` has neither 402 nor 503. `info.description` contains `never return HTTP 402`, and `paths` has no `/api` operation.

### CI failure (open, must fix)

PR #600 `ci (integration)` failed twice, both times with 199 failures in
`web-server-integration-tests` (run 40803144337, attempts 1 and 2). This is deterministic, not a flake.

Cause: `tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs`
declares stub FastEndpoints classes (`TipGetEndpoint`, `TipPostEndpoint`,
`MeteredCapabilityEndpoint`, `HelloEndpoint`) in the shared integration test assembly. Other test
hosts in the same process (for example `HostGraphFactory.CreateWebWithApiAsync`, which starts
api-server through `ApiTestServerApplication`) run FastEndpoints discovery over the loaded
assemblies. They pick up the stubs next to the real endpoints and fail at startup:

```
The route [GET:api/tip] has 2 endpoints registered to handle requests!
The route [POST:api/tip] has 2 endpoints registered to handle requests!
The route [GET:/api/hello] has 2 endpoints registered to handle requests!
The route [GET:api/demo/metered-capability] has 2 endpoints registered to handle requests!
Duplicate routes detected!
   at TimeWarp.Architecture.Api.Server.Program.ConfigureMiddleware (api-server/program.cs:130)
```

Fix so the stubs cannot reach any other host's discovery (or the test needs no endpoint classes),
then prove it by running the whole integration group, not only this file:
`dotnet run tools/dev-cli/dev.cs -- test --group integration`.

## Review

- Round 1 (effort 2, roster: general). The documented 402 and 503 shapes match the handlers and
  the x402 payload. Counts: 0 bug, 2 suggestion, 1 nit. M1: `resource.url` was hard-coded.
  M2: the test stubs hard-coded routes. M3: a dead `?? "/api/tip"` fallback.
- Fix pass: M1 reads `IOptions<TipOptions>` and `IOptions<MeteredCapabilityOptions>` from
  `context.ApplicationServices`. M2 builds the stub routes and asserted paths from the contract
  `RouteTemplate` constants. M3 removed the `CanonicalTipPath` fallback with M1. Smoke filter
  `OpenApiPaymentResponses_` passed 6 (2026-10-29).
- Round 2 (effort 2, roster: general) re-verified the fix delta `1e59e8258..0264769f3`. M1,
  M2, and M3 are fixed, and there are no new findings. `--filter-class OpenApi` passed 23 of 23
  (2026-10-29).
- Final counts: bug 0/0/0, suggestion 0 open / 2 fixed / 0 wontfix, nit 0 open / 1 fixed /
  0 wontfix.
- Disposition: **clean** (2 rounds, 0 open, no wontfix, no escalations).
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Review

- Round 1: M1, M2, M3 implemented in the fix pass.
- Round 2: Done
## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json +
https://arch.timewarp.work/llms.txt Payment section, 2026-10-28.
