# Review Framework — Task 147-007 (phase F close-out)

## Diff scope

Main implementation commit `4c714bf3` (2026-08-04) as it exists on dev TODAY — i.e. including
later amendments: task 155 (wait edge removed; own review kitchen, disposition clean) and task
104-035 (dual-mode TimeWarp.402, found while running this task's smoke gate).

EXCLUDED from this review: AppHost wait-edge semantics — already reviewed and dispositioned
under task 155's review kitchen.

## Roster / effort

Effort 1 — one general reviewer (sonnet), read-only. Surfaces: committed migration + snapshot
vs entity configurations, design-time factory, membership-targets/.editorconfig carve-outs,
postgres-db-module, package pins/assets, scripts + docs, EnsureCreated eradication.

## Gates (run by orchestrator during this close-out)

- `./bin/dev build` 0/0 (after 104-035 wiring)
- `web-infrastructure-tests` 45/45
- `aspire-tests` 7/7 (as of task 158's fix; migration resource in graph)
- `dev template-smoke` — first run exposed the pre-existing 104-011 template breakage (task
  104-035, fixed); rerun pending at framework-writing time; result recorded in task Results
- Schema-change smoke — queued after template-smoke

## Session

- Round 1 reviewer spawned 2026-08-05 by orchestrator (claude); findings returned same day.
