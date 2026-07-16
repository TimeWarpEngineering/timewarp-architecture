# Implement credit ledger keyed by PrincipalId

## Parent

104

## Description

After settle, credit a principal; metered work debits; balance query. Idempotent application of payment receipts. This is the economic limiter for agents.

## Requirements

- Credit on settle
- Debit on use
- Balance
- Idempotent settle application
- Bound to PrincipalId from Identity

## Checklist

- [ ] Ledger model + store
- [ ] Credit/debit/balance API
- [ ] Tests

## Notes

No human required — agent PrincipalId is enough.

### Depends on

104-008, 104-002

## Session

- Created: 2026-07-16
