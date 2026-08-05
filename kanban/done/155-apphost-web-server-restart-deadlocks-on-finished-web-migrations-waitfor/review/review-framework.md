# Review Framework — Task 155

## Diff scope

Commit `cf4266b4` — fix(aspire): remove web-server wait edge on web-migrations:
- `source/container-apps/aspire/projects/aspire-app-host/program.cs` (wait edge removed,
  comment + Design region rewritten)
- `tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs` + `global-usings.cs`
  (SetupOnce terminal-state poll on web-migrations)
- `documentation/developer/how-to-guides/how-to-add-your-aggregate.md` (two spots)
- `documentation/.../approved/0009-postgres-ef-golden-persistence-path.md` (amendment)

## Roster / effort

Effort 1 (single general reviewer, sonnet) — change is small and already heavily scrutinized
(plan agent decompilation pass, implementer, orchestrator gate re-verification, live restart
validation). Reviewer focus: correctness of the no-wait-edge topology across template flag
combinations, comment/Design-region truthfulness, test-wait robustness (timeout, failure
states), doc consistency.

## Gates already verified by orchestrator (do not re-run)

- `./bin/dev build` 0/0
- aspire-tests 6/7; single failure proven pre-existing on baseline (stash → identical failure →
  pop); tracked as task 158
- web-spa-integration-tests 15/15 (+1 pre-existing quarantine), api-server 1/1 (implementer runs)
- Live `dev run`: dashboard restart of web-server with web-migrations Finished → Running,
  no web-migrations wait in logs

## Session

- Round 1 reviewer spawned 2026-08-05 by orchestrator (claude).
