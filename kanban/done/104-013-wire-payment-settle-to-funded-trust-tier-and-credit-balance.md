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

- [x] Integration hook Identity ↔ 402
- [x] Tests for tier + balance after settle

## Notes

Core abuse model: Keyed free, Funded can work.

### Depends on

104-006, 104-012

## Session

- Created: 2026-07-16
- Implement + close: 2026-08-04

## Results

### Summary

**Decision:** shared `SettlementFundingService` in TimeWarp.X402 (not ad-hoc host hooks only).
`PaymentGate` stays pure verify/settle; any principal-bearing paid path calls funding after
`PaymentSettled`. Metered gate is the first consumer; voluntary tip stays principal-less.

- **Library** `funding/SettlementFundingService` + `SettlementFundingResult`:
  - Always `ICreditLedger.CreditAsync(receiptId)` (idempotent)
  - Promote `TrustTier.Funded` when principal exists, not quarantined, and strictly below Funded
  - Missing principal / quarantined / already Funded+ → credit only (never fail after chain settle)
  - ConcurrencyConflict on UpdatePrincipal → one retry then credit-wins
- **MeteredCapabilityGate** settle path: `ApplyAsync` then `DebitAsync` (was direct Credit+Debit)
- **Debit never demotes tier** — documented on SettlementFundingService, ICreditLedger,
  TrustTier, package overview, metered Design regions. Funded = "has settled," not "has balance."
- **DI** web-server: `SettlementFundingService` + `MeteredCapabilityGate` **scoped** (safe with
  EF `IPrincipalStore`); `PaymentGate` / ledger remain singleton.

### Build / tests

- Library + `timewarp-402-tests`: **50/50** (prior + 7 funding service + 1 metered tier case)
- Host `web-jaribu-tests` filter InvokeMeteredCapability: **5/5** (settle asserts Funded + zero
  balance still Funded)

### Review

clean, effort 1

### Next

104-015 rate limits; 104-014 agent E2E path

### How to validate

**Automated**
```bash
cd tests/libraries/timewarp-402-tests && dotnet test -c Release
# expect: SettlementFundingService / Funded promotion cases green
dotnet run source/container-apps/web/features/metered-capability/invoke-metered-capability/invoke-metered-capability-tests.cs
# expect: settle path leaves TrustTier.Funded; debit does not demote
```

**Expect**
- After mock settle on metered path: principal `TrustTier.Funded` and ledger credited (then debited for price)
- Zero balance after debit still Funded (documented rule)

**Not in scope:** tip jar tier promotion (tip has no principal).

