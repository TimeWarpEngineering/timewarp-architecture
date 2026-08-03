# Payment package tests with mocked facilitator

## Parent

104

## Description

Automated tests for challenge/settle/ledger without live chain. Optional Sepolia walkthrough in Results/docs when manual settle needed — CI stays mocked.

## Requirements

- Mock facilitator
- Cover 402/503/200 paths
- CI-safe

## Checklist

- [x] Mock facilitator tests
- [x] Ledger tests
- [x] Tip + meter paths covered

## Notes

Wave 2 exit criterion.

### Depends on

104-008 … 104-011

## Session

- Created: 2026-07-16
- Implement + close: 2026-08-04 overnight

## Results

### Summary

Wave 2 package exit gate for **TimeWarp.402**: expanded `tests/libraries/timewarp-402-tests`
to **42** CI-safe tests (was 18). Shared `MockFacilitator` (no live chain). Covered
library outcomes that hosts map to **503 / 402 / 200** for payment gate, tip path, and
metered capability gate; ledger isolation/idempotency; `HttpFacilitatorClient` against a
stub `HttpMessageHandler`.

Hardening: `HttpFacilitatorClient` now treats empty/non-JSON facilitator bodies as absent
payload (map to `facilitator_http_*` / empty_* reasons) instead of throwing `JsonException`
into the gate.

### Coverage map (library outcomes → host HTTP)

| Surface | 503 Unavailable | 402 Challenge/Rejected | 200 Settled/Granted |
|---------|-----------------|------------------------|---------------------|
| PaymentGate | disabled, misconfigured | unpaid, bad verify, failed settle, malformed sig | mock settle |
| Tip path (`/api/tip`) | disabled | unpaid, rejected | mock settle + PAYMENT-RESPONSE |
| MeteredCapabilityGate | disabled, bad price | unpaid, rejected | prepaid debit; settle→credit→debit |
| Ledger | n/a | n/a | credit/debit/idempotent/isolation |
| HttpFacilitatorClient | n/a | verify empty body → invalid | verify/settle/supported + auth headers |

### Residuals (documented, not blocking Wave 2 package exit)

- **104-009 tip host** still open: free-route isolation, tip endpoint wiring, buyer smoke
  docs. Library tip path is covered; tip host e2e stays on 009.
- **Meter host** already landed with 104-011 (`invoke-metered-capability-tests.cs` —
  unpaid 402, prepaid 200, mock settle 200). Not re-run here as package gate.
- **Full `./bin/dev build`** currently fails on concurrent WIP outside this task
  (104-009 tip contracts double file-scoped namespace; 104-030 api identity-host
  reference). Package + suite: **0/0 build, 42/42 tests**.

### Manual Sepolia (optional, not CI)

When a human needs a live settle walkthrough: configure `PayTo` + testnet facilitator
(`FacilitatorUrls.X402Org`), enable the paid surface, use a Base Sepolia wallet buyer.
CI must remain on mocks.

### Files

| Area | Path |
|------|------|
| Shared mock | `tests/libraries/timewarp-402-tests/mock-facilitator.cs` |
| Gate / tip / meter / ledger / config | `*-tests.cs` under same project |
| Challenge builder | `payment-challenge-builder-tests.cs` |
| HTTP client | `http-facilitator-client-tests.cs` |
| Client harden | `source/libraries/timewarp-402/facilitator/http-facilitator-client.cs` |

### Build / tests

- `dotnet build source/libraries/timewarp-402` + `tests/libraries/timewarp-402-tests`: **0/0**
- `cd tests/libraries/timewarp-402-tests && dotnet test -c Release`: **42/42 passed**

### Next

Wave 2 remaining: 009 tip host, 016 passkey demo, 030 api bearer (if still needed).
Wave 3: 013 settle→Funded, 015 rate limits, 014 agent E2E.
