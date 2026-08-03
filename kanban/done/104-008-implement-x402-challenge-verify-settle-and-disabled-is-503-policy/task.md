# Implement x402 challenge verify settle and disabled-is-503 policy

## Parent

104

## Description

Core protocol: unpaid → PAYMENT-REQUIRED (HTTP 402) with correct shapes for buyers; verify + settle via facilitator abstraction (testnet facilitator and CDP-shaped config later). If tips/metering disabled or misconfigured → **503**, never 402. Free routes must not go through this middleware.

## Requirements

- Challenge builder
- Verify + settle
- Facilitator interface (swap x402.org / CDP)
- Disabled → 503 JSON error
- No private merchant keys in repo

## Checklist

- [x] Challenge (`PaymentChallengeBuilder` + v2 `PAYMENT-REQUIRED` Base64 JSON)
- [x] Verify/settle (`PaymentGate` + `IFacilitatorClient` / `HttpFacilitatorClient`)
- [x] 503 policy (`PaymentConfigEvaluator` → `PaymentUnavailable` / `PaymentErrorPayload`)
- [x] Design region: free never 402 (gate + overview)

## Notes

See timewarp-software tip spike policies.

### Depends on

104-007

### Implementation plan (104-008) — overnight 2026-08-04

Library-only protocol core in `TimeWarp.X402` (no ASP.NET). Port policy from
timewarp-software tip jar. Host wiring deferred to 009/011.

## Session

- Created: 2026-07-16
- Plan + implement + review: 2026-08-04 overnight

## Results

### Summary

Implemented host-agnostic x402 core in **TimeWarp.402** (`TimeWarp.X402`): config
evaluator (disabled/misconfigured → never challenge), challenge builder (v2
`PAYMENT-REQUIRED` Base64 JSON), facilitator port + HTTP client (x402.org-shaped
`/verify` `/settle` `/supported`, optional auth header factory for CDP later),
and `PaymentGate` that orchestrates unpaid→challenge, signature→verify→settle.
Policy and Design regions encode **free routes never 402**; disabled/misconfigured
map to 503 payloads only.

### Files changed (high level)

| Area | Path |
|------|------|
| Options / policy | `source/libraries/timewarp-402/options/*` |
| Protocol | `source/libraries/timewarp-402/protocol/*` |
| Facilitator | `source/libraries/timewarp-402/facilitator/*` |
| Gate | `source/libraries/timewarp-402/gate/*` |
| Tests | `tests/libraries/timewarp-402-tests/*` (8 tests) |
| Wiring | slnx, template exclude, smoke vendored trees |

### Key decisions

- No ASP.NET in package — hosts map `PaymentGateOutcome` to HTTP (009+)
- No Identity/PrincipalId dependency (013 composition)
- No merchant keys; only public `payTo` + facilitator URL/auth factory
- Wire payload for payment signatures stays `JsonElement` (scheme-agnostic)
- Namespace remains `TimeWarp.X402` (PackageId `TimeWarp.402`)

### Build / tests

- `./bin/dev build`: **0/0**
- `cd tests/libraries/timewarp-402-tests && dotnet test -c Release`: **8/8 passed**

### Review

- Effort 1, round 1 general; disposition **clean**

### Next

104-009 tip-jar example; 104-010 credit ledger; 104-016 can parallel
