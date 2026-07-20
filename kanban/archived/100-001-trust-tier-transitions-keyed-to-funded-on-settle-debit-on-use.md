# Trust tier transitions Keyed → Funded on settle; debit on use

## Parent

100

## Description

Wire payment events into Identity trust tiers and quota claims. Settled payment raises tier / credits; usage debits.

## Requirements

- Event or direct call from 402 settle → Identity/ledger
- Tier enum transitions documented

## Checklist

- [ ] Integration hook
- [ ] Tier update rules
- [ ] Tests

## Session

- Created: 2026-07-16
