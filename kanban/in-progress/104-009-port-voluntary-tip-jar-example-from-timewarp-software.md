# Port voluntary tip-jar example from timewarp-software

## Parent

104

## Description

Teachable voluntary tip: GET/POST tip resource only 402s when enabled; rest of site free. Port patterns from timewarp-software worker/tip.js + tip-buyer + tests. Prefer architecture-local example (Worker and/or .NET host — choose pragmatic). Discovery alias optional (scanners probing /api).

## Requirements

- Free content never 402 from tip middleware
- Enabled unpaid tip → 402
- Disabled → 503
- Document local run (testnet)

## Checklist

- [x] Example host + tip endpoint
- [x] Config vars (TIP_ENABLED, PAY_TO, network, price)
- [x] Buyer smoke script or doc
- [x] Unit tests with mocked facilitator

## Notes

Production mainnet can wait; testnet teachable first. Source: timewarp-software.

### Depends on

104-008

## Session

- Created: 2026-07-16
- Implement + review: 2026-08-04

## Results

### Summary

Voluntary x402 tip jar on **web-server** (distinct from metered 104-011 — no ledger, no agent auth):

- **Endpoint** `GET|POST api/tip` (`SubmitTip` / `SubmitTipPost`), `[EndpointAllowAnonymous]`.
  Uses `PaymentGate` only: enabled unpaid → **402** + `PAYMENT-REQUIRED`; disabled → **503**
  `tips_disabled` (never 402); settled → **200** thank-you + `PAYMENT-RESPONSE`.
- **Config** `TipOptions` (Enabled false by default; Development enables + public dead PayTo).
  Env overlay via `TipEnvironment`: `TIP_ENABLED` (strict `"true"`), `TIP_PAY_TO`,
  `TIP_NETWORK`, `TIP_PRICE`, `TIP_FACILITATOR_URL`, `TIP_ASSET`, plus CDP key presence →
  `HasFacilitatorAuth` / mainnet `RequiresFacilitatorAuth` (eip155:8453).
- **Shared** `IPaymentHttpContext` / `HttpPaymentHttpContext` extracted to Features substrate
  (`features/payment/`) so tip + metered share Payment-* header I/O without TWA0009.
- **Local testnet**: Development config or `TIP_ENABLED=true` + `TIP_PAY_TO=<sepolia address>`;
  facilitator `https://x402.org/facilitator`; `curl -si https://localhost:7000/api/tip` → 402.
  Free routes never enter the tip handler. Optional settle: timewarp-software `tools/tip-buyer`
  against this host (documented in handler Design region).

### Build / tests

- `./bin/dev build`: **0/0**
- `submit-tip-tests.cs` standalone: **7/7** (4 integration + 3 TipEnvironment unit)
- `web-jaribu-tests` filter Tip: **7/7**
- Metered regression filter InvokeMeteredCapability: **5/5**
- `timewarp-402-tests`: **42/42** (PaymentGate already covers tip-shaped Ready options)

### Review

clean, effort 1

### Next

104-020 discoverable x402 path; 104-022 tip in E2E sunny paths
