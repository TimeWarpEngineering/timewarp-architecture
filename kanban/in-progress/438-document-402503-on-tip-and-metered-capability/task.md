# Document 402/503 on tip and metered-capability

## Description

Per `llms.txt`/`auth.md`, `GET|POST /api/tip` returns 402+PAYMENT-REQUIRED
when payment is enabled (503 when disabled), and
`GET /api/demo/metered-capability` needs Bearer `demo:invoke`, returning 402
or 200-with-debit (503 when disabled). The OpenAPI documents only 200 for
both, with no scope hint.

## Goal (implementer restatement)

The OpenAPI document for this host should tell the truth that `llms.txt` and `auth.md` already tell. `GET` and `POST /api/tip` can answer 402 with an x402 `PAYMENT-REQUIRED` challenge when payment is enabled, and 503 when payment is disabled or misconfigured. `GET /api/demo/metered-capability` is the same pair of payment outcomes, and the operation must say the caller needs bearer scope `demo:invoke` (a 200 on that route is a ledger debit). Free and discovery routes, including fetching the OpenAPI document itself, still never return 402. The bare `/api` path stays a tip-jar alias, not a separate operation and not a free route.

## Checklist

- [x] Add 402 (x402 challenge shape) and 503 responses to /api/tip (GET+POST)
- [x] Add 402/503 + demo:invoke scope note to /api/demo/metered-capability
- [x] Keep free/discovery never-402 statement accurate

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)
- Implementation: task-work implement oracle (2026-10-29)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a18528-d963-7fb2-9d37-b4a636af295b, max-turns 500, Done — 2026-10-29T01:10:04Z

## Results

The web OpenAPI document now lists HTTP 402 and 503 on `GET` and `POST /api/tip` and on `GET /api/demo/metered-capability`. The 402 body is `application/problem+json`. The `PAYMENT-REQUIRED` header is a base64 x402 v2 object (`x402Version`, `accepts` with `scheme`, `network`, `payTo`, `price`, and `resource.url`). 503 is `application/problem+json` with `error`, `payment`, and `ok`, and it has no `PAYMENT-REQUIRED` header. The metered operation requires bearer scope `demo:invoke` and its 200 description says the call debits the ledger (`balanceAfter`). The document description states that free and discovery operations never return 402, and that bare `/api` is a tip-jar alias rather than its own operation. `llms.txt` and `auth.md` say the catalog lists those codes on the paid operations only, and that fetching `/openapi/v1.json` never returns 402.

### How to validate

Smoke: from `tests/container-apps/web/web-server-integration-tests` (its `global.json` selects the Microsoft.Testing.Platform runner), `dotnet test web-server-integration-tests.csproj -- --filter-class OpenApiPaymentResponses_`.

Expect: 5 passed. `GET` and `POST /api/tip` are anonymous and document 402 (header `PAYMENT-REQUIRED` required, description and schema include `x402Version`, `accepts`, `payTo`, and `base64`) and 503 (no `PAYMENT-REQUIRED`). `GET /api/demo/metered-capability` documents the same pair, `security` scope `demo:invoke`, and a 200 description that contains `balanceAfter`. `GET /api/hello` has neither 402 nor 503. `info.description` contains `never return HTTP 402`, and `paths` has no `/api` operation.

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json +
https://arch.timewarp.work/llms.txt Payment section, 2026-10-28.
