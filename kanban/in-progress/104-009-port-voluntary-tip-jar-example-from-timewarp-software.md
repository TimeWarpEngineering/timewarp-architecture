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

- [ ] Example host + tip endpoint
- [ ] Config vars (TIP_ENABLED, PAY_TO, network, price)
- [ ] Buyer smoke script or doc
- [ ] Unit tests with mocked facilitator

## Notes

Production mainnet can wait; testnet teachable first. Source: timewarp-software.

### Depends on

104-008

## Session

- Created: 2026-07-16
