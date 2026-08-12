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

- [x] Ledger model + store (`ICreditLedger`, `InMemoryCreditLedger`)
- [x] Credit/debit/balance API
- [x] Tests (5 ledger tests in timewarp-402-tests)

## Notes

No human required — agent PrincipalId is enough.

### Depends on

104-008, 104-002

## Session

- Created: 2026-07-16
- Implement + review: 2026-08-04 overnight

## Results

### Summary

Added PrincipalId-keyed credit ledger to TimeWarp.X402: `ICreditLedger` with
idempotent `CreditAsync(receiptId)`, fail-closed `DebitAsync`, and
`GetBalanceAsync`. In-memory implementation for demos/tests. Package now
references TimeWarp.Identity for `PrincipalId` (dual-mode).

### Build / tests

- Library build 0/0
- timewarp-402-tests: **13/13** (8 prior + 5 ledger)

### Review

clean, effort 1

### Next

104-011 metered demo; 104-009 tip jar; 104-013 settle→Funded

### How to validate

**Automated**
```bash
cd tests/libraries/timewarp-402-tests && dotnet test -c Release -- --filter-class InMemoryCreditLedger
# or full suite:
cd tests/libraries/timewarp-402-tests && dotnet test -c Release
# expect: credit / idempotent receipt / debit / insufficient fail-closed tests green
```

**Expect**
- `ICreditLedger` + `InMemoryCreditLedger` under `source/libraries/timewarp-402/ledger/`
- Keys are `TimeWarp.Identity.PrincipalId` (package dual-mode ref to Identity)
- Same receipt id twice does not double-credit

**Not in scope:** EF durable ledger; host settle→credit wiring (013).

