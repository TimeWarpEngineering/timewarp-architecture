# ADR, HowToAddYourAggregate, and 113 closeout verification

## Description

Child of [[113-golden-persistence-implementation-postgres-first-aspire-wired-with-actor-model-evaluation]]
documentation + dual-flag verification closeout. Lands after 113-003 and 113-004.

## Requirements

- ADR (next after 0008): `documentation/developer/conceptual/architectural-decision-records/approved/`
  — Postgres-only; state-store EF; GoldenDbContext in Foundation; schema-per-slice via tables on
  one context; actors optional Orleans later; outbox deferred; identity first product consumer
  via 104-032; EnsureCreated vs Migrate; two-party Version contract
- How-to: `documentation/developer/how-to-guides/HowToAddYourAggregate.md` — domain → config →
  SaveChanges → tests; when to use store port vs direct DbContext; when to earn Orleans
- Update `web-domain/aggregates/overview.md` and Design regions as needed
- Parent Notes: 104-032 unblocked as first product consumer; dual-fixture store-contract pattern
- Verify: `dev build` 0/0 and `dev test` green with postgres flag **on AND off**
- Parent checklist complete; Results on parent before done

## Checklist

- [ ] ADR approved
- [ ] HowToAddYourAggregate.md
- [ ] Overview / Design region updates
- [ ] Parent Notes + 104-032 unblock note
- [ ] `dev build` / `dev test` both flag modes
- [ ] Parent Results ready for orchestrator Phase 5

## Notes

- Do not implement Orleans, outbox, or 104-032 here.

## Session

- Created: 2026-07-23 (from 113 remaining-work plan)
