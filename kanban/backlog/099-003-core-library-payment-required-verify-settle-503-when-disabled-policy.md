# Core library PAYMENT-REQUIRED, verify, settle, 503-when-disabled

## Parent

099

## Description

Implement TimeWarp.402 core: build payment-required responses, verify and settle via facilitator, enforce disabled/misconfigured → 503 never 402.

## Requirements

- Correct header/body shapes for buyer interop (x402 v2)
- Facilitator client(s)
- Policy helpers for free vs paid routes

## Checklist

- [ ] Challenge builder
- [ ] Verify + settle
- [ ] 503 policy
- [ ] Package tests

## Session

- Created: 2026-07-16
