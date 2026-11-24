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

## Checklist

- [ ] Implement the requirements above
- [ ] Tests or document checks named above pass
- [ ] Re-fetch the live document after deploy and confirm the finding is gone

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-10 and M-12 (Grok G-007, G-011) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.

## Session

- Created: 2227132 (2026-11-24)
