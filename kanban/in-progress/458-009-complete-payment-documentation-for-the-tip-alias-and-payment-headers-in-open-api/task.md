# Complete payment documentation for the tip alias and payment headers in OpenAPI

## Description

`GET` and `POST /api` (the tip-jar alias) publish 200/402/503 as description-only responses: no
content, no `PAYMENT-REQUIRED` header. `PAYMENT-REQUIRED` is documented on `/api/tip`, but the
client request header `PAYMENT-SIGNATURE` and the success header `PAYMENT-RESPONSE` appear nowhere
in the document. `info.description` appends the payment paragraph to the last exclusion bullet with
one space, and says 402/503 are documented only on tip and the metered capability while
`GET /api/health` documents a 503.

## Requirements

- Build the alias operations by cloning the tip operations' responses (content, headers).
- Add optional request header `PAYMENT-SIGNATURE` and response header `PAYMENT-RESPONSE` on the tip
  operations and `GET /api/demo/metered-capability`, from the `PaymentHeaders` constants.
- Append the payment note as its own paragraph; generate the "documented only on" list from the
  operations that carry 402; distinguish the health 503.
- Tests over the document only. Do not call paid routes against the live host.

Owner: transformers `open-api-discovery-coverage.cs` and `source/container-apps/web/platform/payment/open-api-payment-responses-server.cs`.

## Goal (implementer restatement)

The web OpenAPI document should describe x402 payment fully and consistently.

- `GET` and `POST /api` (the tip-jar rewrite alias) carry the same responses as the `/api/tip`
  operation of the same verb: the 402 body and `PAYMENT-REQUIRED` header, the 503 body, and the
  200. They are copied from the generated tip operations, not hand-written again.
- The tip operations and `GET /api/demo/metered-capability` document the optional request header
  `PAYMENT-SIGNATURE` and the optional success header `PAYMENT-RESPONSE`, named from
  `PaymentHeaders`. The alias gets the same request header through the copy.
- `info.description` gets the payment note as its own paragraph (blank line before it). The list
  of operations that document 402 is read from the document, not typed. The note says the 503 on
  `GET /api/health` is a health status, not a payment response.

Acceptance: document-only tests (no live paid calls) in `open-api-payment-responses-tests.cs`
show the alias responses match tip (content, headers), the two payment headers on all paid
operations, the paragraph break, the generated 402 list, and the health 503 sentence.
`./bin/dev build` is clean and `ganda repo audit` passes.

## Checklist

- [x] Implement the requirements above
- [x] Tests or document checks named above pass

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-10 and M-12 (Grok G-007, G-011) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.
- Post-deploy verification: re-fetch the live OpenAPI document after deploy and confirm the finding is gone.

## Session

- Created: 2227132 (2026-11-24)
- Implemented 2026-11-26: alias cloned from tip operations in `OpenApiDiscoveryCoverage`; payment
  headers and generated info paragraph in `OpenApiPaymentResponses`.

## Results

What changed:

- `source/foundation/foundation-server/open-api-discovery-coverage.cs`: `GET`/`POST /api` are
  built from the `/api/tip` operation of the same verb: shallow copies of every response
  (content and headers), the parameters, and the request body. The document transformer runs
  after all operation transformers, so the host's payment 402/503 and headers are included. With
  no `/api/tip` operation the alias falls back to one description-only 200.
- `source/container-apps/web/platform/payment/open-api-payment-responses-server.cs`: tip GET/POST
  and the metered GET get optional request header `PAYMENT-SIGNATURE` and optional 200 header
  `PAYMENT-RESPONSE` (base64 JSON), named from `PaymentHeaders`. The info note is now its own
  `## Payment` paragraph after a blank line. Its "documented only on" list is generated from the
  operations that carry a 402; every other operation with a 503 (`GET /api/health`) is named as
  that route's own status, not a payment response.
- `tests/.../open-api-payment-responses-tests.cs`: host turns on `documentPublicWebRoutes`, as
  web-server does. New document-only tests: paragraph break, exact 402 list, health 503 sentence,
  alias responses/parameters equal tip's, payment headers on all five paid operations and absent
  on a free route. No paid route is called.
- Audit: `vscode-window-icon` advisory fixed by `ganda repo audit --fix` (writes the git-ignored
  `.vscode/settings.json`; nothing to commit).

Gates:

- `./bin/dev build` — passed, 0 warnings, 0 errors.
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs` — passed 11/11.
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-discovery-coverage-tests.cs` — passed 8/8 (covers the edited `OpenApiDiscoveryCoverage`).
- Neither test file is a standalone runfile (no `#if !JARIBU_MULTI` guard); both ran through `./bin/dev test <file>`.
- `ganda repo audit` — passed with one advisory (`vscode-window-icon`), fixed by `ganda repo audit --fix`.

### How to validate

Smoke: `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs`
After deploy: fetch `https://arch.timewarp.work/openapi/v1.json`.

Expect: all tests pass. In the document, `paths./api.get` and `paths./api.post` have the same
`responses` (402 with `PAYMENT-REQUIRED` header and problem+json content, 503 content) and
`parameters` as `/api/tip`; tip, alias, and metered operations list header parameter
`PAYMENT-SIGNATURE` (not required) and 200 header `PAYMENT-RESPONSE`; `info.description` has a
blank line then `## Payment`, lists exactly `GET /api`, `POST /api`, `GET /api/tip`,
`POST /api/tip`, `GET /api/demo/metered-capability` as documenting 402, and says the 503 on
`GET /api/health` is not a payment response.

