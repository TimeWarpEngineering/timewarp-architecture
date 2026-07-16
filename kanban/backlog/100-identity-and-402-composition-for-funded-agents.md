# Identity and 402 composition for funded agents

## Description

Compose TimeWarp.Identity and TimeWarp.402 so agents (and humans) earn power via
stake: Keyed → Funded on settle; debit on use; cheap free sybils cannot do
expensive work. Optional human link / humanUx — not required for paid service.

## Requirements

- Trust tier transitions driven by payment events
- End-to-end agent path: register key → 402 → pay → scoped token with quota
- Abuse defaults: rate limits on register/challenge; cheap 402 responses
- Optional humanUx handoff payload for agent→human presentation

## Checklist

- [ ] 100-001 Trust tier transitions + debit
- [ ] 100-002 Agent happy path
- [ ] 100-003 Abuse defaults
- [ ] 100-004 Optional human link / humanUx

## Notes

### Depends on
098 (principals/tokens), 099 (settle/credits).

### Unblocks
103 E2E sunny paths.

## Session

- Created: 2026-07-16
