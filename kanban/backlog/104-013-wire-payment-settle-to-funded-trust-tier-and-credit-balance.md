# Wire payment settle to Funded trust tier and credit balance

## Parent

104

## Description

Composition: successful settle updates principal TrustTier to Funded (or equivalent) and credits ledger. Token/session claims can carry quota snapshot.

## Requirements

- Settle → tier transition
- Settle → credit
- Debit does not necessarily demote tier immediately (document rule)

## Checklist

- [ ] Integration hook Identity ↔ 402
- [ ] Tests for tier + balance after settle

## Notes

Core abuse model: Keyed free, Funded can work.

### Depends on

104-006, 104-012

## Session

- Created: 2026-07-16
