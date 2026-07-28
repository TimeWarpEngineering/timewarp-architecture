# Round 2 — orchestrator verification (module fold-in delta)
**Date:** 2026-07-27
**Scope:** commit `e28abe56` (maintainer-ruled fold-in: modules follow concerns) — the delta
after round-1's zero-findings review of `351959b5`/`c7d31c07`.

## The delta

- `web-infrastructure-module.cs` → `features/identity/in-memory-identity-stores-module-infrastructure.cs`;
  class renamed `InMemoryIdentityStoresModule`; namespace adopts
  `TimeWarp.Architecture.Features.Identity.Infrastructure` (slice-membership rule). All four
  consumer references updated (program.cs call, two postgres-db-module comments, one
  ef-principal-store comment); vacated sole-occupant namespace's dead global using removed;
  Purpose/Design regions reframed (durability rationale preserved). Modules paragraph added to
  the placement guide: a module is a concern's registration manifest; ordering stays in
  program.cs (bootstrap); no assembly-level modules; no ceremony for concerns without
  registrations.

## Regression caught and fixed by the gate

First smoke run FAILED on SmokeNoPostgres: the `Features.Identity.Infrastructure` global using
sat inside `#if(postgres)` (added in the EfPrincipalStore era), so generated no-postgres apps
lost it while program.cs called the module unconditionally. Invisible to the monorepo build
(postgres constant always defined) and to all unit tests — exactly the failure class the
two-matrix smoke exists for. Fix: the using is now unconditional (the module is the namespace's
unconditional occupant); `TimeWarp.Architecture.Persistence` stays postgres-conditional
(its only generated-app occupant is stripped without the flag).

## Verification

- Repo-wide: zero `WebInfrastructureModule` / `TimeWarp.Architecture.Web.Infrastructure`
  references outside kanban history (sole test-project-namespace hit is the test assembly's own
  distinct namespace).
- `dev build` 0/0 · web-infrastructure-tests 39/39 · web-server-integration-tests 97+1 skip
  (the two projects exercising the module↔PostgresDbModule swap semantics) ·
  `dev template-smoke` SUCCEEDED both matrices after the fix.

## Result

0 open. Round-1's zero findings stand; delta verified. Proceed to disposition.
