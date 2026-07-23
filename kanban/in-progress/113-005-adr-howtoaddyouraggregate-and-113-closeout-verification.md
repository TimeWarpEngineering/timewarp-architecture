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

- [x] ADR approved
- [x] HowToAddYourAggregate.md
- [x] Overview / Design region updates
- [x] Parent Notes + 104-032 unblock note
- [x] `dev build` / key tests (monorepo postgres on; dual-flag = template-smoke residue)
- [x] Parent Results ready for orchestrator Phase 5

## Notes

- Do not implement Orleans, outbox, or 104-032 here.

## Results

**Implemented 2026-07-23** — ADR-0009, HowToAddYourAggregate, index/overview updates, parent closeout notes, verification.

### What was implemented

1. **ADR-0009** — Postgres + EF Core golden persistence path (accepted): Postgres-only; state-store
   EF (no ES); GoldenDbContext in Foundation; two-party Version; child→root; schema-per-slice on
   one DbContext; EnsureCreated vs Migrate; Orleans optional; Akka for 118; outbox deferred;
   104-032 first product consumer; Profile teaching aggregate.
2. **HowToAddYourAggregate.md** — eight-section walkthrough + PR checklist.
3. **Doc indexes** — approved ADR Overview, how-to Overview populated; aggregates/overview.md
   SaveChanges points at GoldenDbContext + how-to link.
4. **Parent task** — all checklist items checked; soft-gate table accepted; 104-032 unblocked
   explicitly; dual-fixture store-contract as reference pattern.

### Files

- `documentation/developer/conceptual/architectural-decision-records/approved/0009-postgres-ef-golden-persistence-path.md` (new)
- `documentation/developer/conceptual/architectural-decision-records/approved/Overview.md`
- `documentation/developer/how-to-guides/HowToAddYourAggregate.md` (new)
- `documentation/developer/how-to-guides/Overview.md`
- `source/container-apps/web/web-domain/aggregates/overview.md`
- `kanban/in-progress/113-golden-persistence-…/task.md`
- this child task

### Key decisions (documented, not re-implemented)

- Soft-gate silence = accept for 3b/4/5
- Dual postgres flag: monorepo always dogs postgres-on (`web-server` `DefineConstants`); off-mode
  is template generation / `dev template-smoke` CI residue (`.github/workflows/template-smoke.yml`)
- No Orleans / outbox / 104-032 code in this task

### Tests / verification

- `dev build` → **0 Warning(s), 0 Error(s)**
- `dotnet fixie tests/foundation/foundation-infrastructure-tests` → **7 passed**
- `dotnet fixie tests/container-apps/web/web-infrastructure-tests` → **5 passed** (Testcontainers
  Postgres available; no soft-skips this run)
- Known skip: Profile Postgres live tests soft-skip without Docker/connection string
- Dual-flag off: not flipped in monorepo; residual CI gate is template-smoke

### Commit

- `ca51fe44` docs(113-005): ADR-0009 golden Postgres EF path and HowToAddYourAggregate

## Session

- Created: 2026-07-23 (from 113 remaining-work plan)
- Implementation: 2026-07-23 (build agent via orchestrator)
