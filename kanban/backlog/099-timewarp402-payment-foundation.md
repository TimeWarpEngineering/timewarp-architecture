# TimeWarp.402 payment foundation

## Description

Internet-native payment package (x402): tip jar, metered capabilities, credit
ledger bound to principals. Extract teachable tip example from timewarp-software;
generalize facilitator abstraction. Free/discovery routes never accidental 402;
disabled → 503.

## Requirements

- Challenge (PAYMENT-REQUIRED), verify, settle
- Facilitator abstraction (x402.org testnet, CDP mainnet, …)
- Credit ledger keyed by PrincipalId (composition with 098/100)
- Tip demo + metered pay-for-capability demo
- Hosting story (Worker and/or Aspire BFF)
- Policy docs: free never 402; misconfigured → 503

## Checklist

- [ ] 099-001 Design API + ledger + facilitators
- [ ] 099-002 Extract tip-jar example from timewarp-software
- [ ] 099-003 Core library
- [ ] 099-004 Credit ledger
- [ ] 099-005 Metered demo endpoint
- [ ] 099-006 Aspire/Worker hosting
- [ ] 099-007 Tests + Sepolia walkthrough

## Notes

### Source material
timewarp-software: worker/tip.js, documentation/x402-tip-spike.md, tip-buyer,
backlog task 019 (extract testnet example).

### Depends on
097-002 ADR; credit binding needs PrincipalId from 098 (can mock until then).

### Unblocks
100, 101-005 commerce discovery.

## Session

- Created: 2026-07-16
