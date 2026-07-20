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

- [ ] Challenge
- [ ] Verify/settle
- [ ] 503 policy
- [ ] Design region: free never 402

## Notes

See timewarp-software tip spike policies.

### Depends on

104-007

## Session

- Created: 2026-07-16
