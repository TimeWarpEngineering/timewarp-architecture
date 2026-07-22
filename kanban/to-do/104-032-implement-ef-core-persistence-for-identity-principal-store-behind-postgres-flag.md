# Implement EF Core persistence for identity principal store behind postgres flag

## Description

Identity is currently in-memory only: `IPrincipalStore` → singleton `InMemoryPrincipalStore`
(`web-infrastructure-module.cs`), no EF Core anywhere in timewarp-identity. Every web-server
restart wipes all principals, passkey credentials, and agent keys — discovered concretely during
task 112's public share: the first external passkey registration (colleague via
arch.timewarp.work, 2026-07-21) evaporated on the next `dev run` restart; their authenticator
still holds the client half, so sign-in fails as "unknown credential" until re-registration.

Goal: EF Core-backed `IPrincipalStore` (and the other identity stores that must survive
restarts) behind the template's `postgres` flag, with in-memory remaining the no-flag default so
the template still runs with zero infrastructure.

## Requirements

- EF persistence lives where the postgres flag already gates EF Core; the timewarp-identity
  library stays persistence-free (store interfaces only) — implementation goes in the
  infrastructure layer alongside the existing EF wiring.
- Same store semantics the in-memory implementation pins down: atomic handle-uniqueness
  (InMemoryPrincipalStore.HandleIndex under WriteLock has a documented contract in
  complete-passkey-registration-handler), optimistic concurrency surfacing as
  `ConcurrencyConflictException`, and byte[]-keyed credential lookups.
- Inventory which stores need durability vs deliberately ephemeral: `IPrincipalStore` (durable),
  `IAgentTokenStore` (short-lived tokens — decide), `IWebAuthnChallengeStore` (ephemeral by
  design — stays in-memory).
- Aggregate mapping respects the golden aggregate pattern (106) and private `Invariants`
  validators (TWA0011/0012) — no EF-driven shape leaks into the domain model.
- Migrations story consistent with the template's existing postgres flag content.
- Existing 168 identity unit tests keep running against in-memory; add integration coverage for
  the EF store (store-contract test suite runnable against both implementations would be ideal —
  one suite, two fixtures).
- Registration: flag-gated DI so `postgres` template output persists and no-flag output keeps
  the singleton in-memory store.

## Checklist

- [ ] Store durability inventory (principal / agent-token / challenge) with decisions recorded
- [ ] EF entity configuration + DbContext integration for principals, credentials, agent keys
- [ ] Store-contract test suite against both in-memory and EF implementations
- [ ] Handle-uniqueness + concurrency-conflict semantics proven under the EF store
- [ ] Flag-gated DI registration (template `#if postgres` consistency)
- [ ] Migrations + template output verified for both flag states
- [ ] Update AGENTS.md / identity ADR if the persistence seam changes documented shape

## Notes

- Origin: task 112 public share made restart-wipes externally visible; blocks meaningful use of
  [[104-010-implement-credit-ledger-keyed-by-principalid]] and
  [[104-013-wire-payment-settle-to-funded-trust-tier-and-credit-balance]] (balances keyed by
  PrincipalId must survive restarts before anyone pays into them).
- Related: [[104-005]] (credential list/revoke UX assumes credentials persist),
  [[104-014-agent-end-to-end-path-register-key-then-pay-then-call-with-quota-token]].

## Session

- Created: 2026-07-21 (spun out of 112 share observations)
