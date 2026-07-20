# Credit ledger bound to PrincipalId

## Parent

099

## Description

Persist credits/debits per principal after successful payment; query balance; reject or 402 when insufficient for metered work.

## Requirements

- Credit/debit/balance API
- Idempotent settle application
- Storage choice documented

## Checklist

- [ ] Ledger model
- [ ] Apply payment → credit
- [ ] Debit helper
- [ ] Tests

## Session

- Created: 2026-07-16
