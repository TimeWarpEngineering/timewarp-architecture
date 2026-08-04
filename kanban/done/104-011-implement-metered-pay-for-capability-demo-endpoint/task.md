# Implement metered pay-for-capability demo endpoint

## Parent

104

## Description

Demo expensive capability: without credit/payment → 402; with balance or payment → 200 and debit. Distinct from voluntary tip (009).

## Requirements

- Clear agent-facing error/pay instructions
- Debits ledger
- Integration test

## Checklist

- [x] Endpoint + policy
- [x] Wire ledger
- [x] Tests

## Notes

Proves payment-as-product, not only tip jar.

### Depends on

104-010

### Host choice (locked overnight)

**web-server** — agent bearer (`AgentTokenDefaults` / `demo:invoke`) already exists there.
api-server bearer is 104-030; placing the meter on api-server would block on 030. Free routes never
go through payment middleware — only `GET api/demo/metered-capability`.

## Session

- Created: 2026-07-16
- Implement + review: 2026-08-04 overnight

## Results

### Summary

Metered pay-for-capability demo on **web-server**:

- **Library** `MeteredCapabilityGate` (TimeWarp.X402): prepaid credit debit first; else
  PaymentGate settle → CreditAsync(receipt) → DebitAsync(price). Disabled/misconfigured →
  Unavailable (503 never 402). Distinct from voluntary tip (009).
- **Endpoint** `InvokeMeteredCapability` `GET api/demo/metered-capability`, policy
  `agent-scope:demo:invoke` (`AgentScopes.DemoInvoke`). Maps outcomes to 200 / 402+PAYMENT-REQUIRED /
  503; ledger debit on every success.
- **Config** `MeteredCapabilityOptions` (Enabled false by default; Development enables + public dead
  PayTo). No merchant private keys.
- **DI** InMemoryCreditLedger, PaymentGate, MeteredCapabilityGate, mockable IFacilitatorClient.

### Build / tests

- `./bin/dev build`: **0/0**
- `timewarp-402-tests`: **18/18** (13 prior + 5 metered gate)
- `web-jaribu-tests` filter InvokeMeteredCapability: **5/5** (402 unpaid, prepaid debit, mock
  settle, 401, 403 insufficient scope)

### Review

clean, effort 1

### Next

104-012 payment package tests exit gate; 104-009 tip jar; 104-013 settle→Funded

### How to validate

**Automated**
```bash
# Co-located metered host tests (Jaribu runfile / aggregator)
dotnet run source/container-apps/web/features/metered-capability/invoke-metered-capability/invoke-metered-capability-tests.cs
# expect: unpaid 402, prepaid debit 200, mock settle 200, 401/403 as designed
cd tests/libraries/timewarp-402-tests && dotnet test -c Release
# expect: MeteredCapabilityGate cases green
```

**Manual smoke** (needs agent bearer with `demo:invoke`; Development enables metered)
```bash
./bin/dev run
# Register agent key + token (see tools/agent-identity-cli demo), then:
curl -si -H "Authorization: Bearer <token>" https://localhost:7000/api/demo/metered-capability | head -30
# expect without credit/payment: 402 + PAYMENT-REQUIRED
```

**Not in scope:** live facilitator; api-server host (meter is web-server).

