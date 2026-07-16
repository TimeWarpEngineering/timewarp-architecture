# Template integration Definition of Done for Identity and 402

## Description

Integrate Identity + 402 + agent surface into the timewarp-architecture template:
feature flags/symbols, slice placement (TWA0009), developer docs, and E2E sunny
paths (human passkey, agent paid call, tip).

## Requirements

- Template can enable demos without breaking default build
- Slices respect isolation rules
- How-to + conceptual docs
- E2E covers three sunny paths

## Checklist

- [ ] 103-001 Feature flags / template symbols
- [ ] 103-002 Slice placement
- [ ] 103-003 Documentation
- [ ] 103-004 E2E sunny paths

## Notes

### Depends on
098, 099, 100; enough of 101 for agent-facing docs/endpoints.

### Definition of Done for program
Template demonstrates: passkey-first human, wallet-funded agent without human,
voluntary tip, agent discovery surfaces.

## Session

- Created: 2026-07-16
