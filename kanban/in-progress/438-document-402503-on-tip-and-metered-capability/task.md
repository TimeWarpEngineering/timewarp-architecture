# Document 402/503 on tip and metered-capability

## Description

Per `llms.txt`/`auth.md`, `GET|POST /api/tip` returns 402+PAYMENT-REQUIRED
when payment is enabled (503 when disabled), and
`GET /api/demo/metered-capability` needs Bearer `demo:invoke`, returning 402
or 200-with-debit (503 when disabled). The OpenAPI documents only 200 for
both, with no scope hint.

## Checklist

- [ ] Add 402 (x402 challenge shape) and 503 responses to /api/tip (GET+POST)
- [ ] Add 402/503 + demo:invoke scope note to /api/demo/metered-capability
- [ ] Keep free/discovery never-402 statement accurate

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json +
https://arch.timewarp.work/llms.txt Payment section, 2026-10-28.
