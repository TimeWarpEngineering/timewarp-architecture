# Agent-ready product vision and ADRs

## Description

Capture product decisions from the Identity / 402 / agent-ready brainstorm so
implementation epics (098–103) share one north star. Humans and agents are
first-class principals; payment (x402) is stake and product feature; Entra is
not the center; score well on https://isitagentready.com/.

## Requirements

- ADRs record locked decisions (multi-principal, 402 as stake, edge vs app)
- North-star doc + v1 isitagentready must-pass checklist
- Explicit non-goals: Entra priority, Passwordless.dev as long-term center,
  full SSI/DID in v1, human required for paid agents

## Checklist

- [ ] 097-001 Identity ADR approved shape
- [ ] 097-002 Payment/trust-tier ADR
- [ ] 097-003 Edge vs app ADR
- [ ] 097-004 North-star + score targets
- [ ] Cross-link children of 098–103 to these ADRs

## Notes

### Locked decisions
1. No human required if agent pays (wallet / x402).
2. Payment is part of the template story.
3. Any human authenticator (e.g. Proton Pass).
4. Browser sessions; short-lived scoped tokens for agents.
5. Hybrid identity: server PrincipalId + keys; SSI later.
6. Package names: TimeWarp.Identity, TimeWarp.402.
7. Cloudflare = outer ring (DDoS/WAF/bots), not identity source of truth.

### Dependency
Unblocks design children under 098 and 099; informs 101/102/103.

## Session

- Created: 2026-07-16 (epic tree from Identity/402 brainstorm)
