# Implement metered pay-for-capability demo endpoint

## Parent

104

## Description

Demo expensive capability: without credit/payment → 402; with balance or payment → 200 and debit. Distinct from voluntary tip (009).

## Requirements

- Clear agent-facing error/pay instructions
- Debits ledger
- Integration test

## Checklist

- [ ] Endpoint + policy
- [ ] Wire ledger
- [ ] Tests

## Notes

Proves payment-as-product, not only tip jar.

### Depends on

104-010

## Session

- Created: 2026-07-16
