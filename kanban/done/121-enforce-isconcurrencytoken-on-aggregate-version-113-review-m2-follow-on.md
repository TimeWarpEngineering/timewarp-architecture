# Enforce IsConcurrencyToken on aggregate Version (113 review M2 follow-on)

## Description

GoldenDbContext's concurrency story was a **two-party contract** (ADR-0009): the golden
SaveChanges hook increments `Version` on Modified aggregate roots, but the optimistic WHERE
clause only existed if the host's entity configuration also mapped
`.Property(x => x.Version).IsConcurrencyToken()`. Forget the mapping and the increment is
silent bookkeeping — lost updates ship with green builds.

## Decision (Steve, 2026-07-24): auto-apply — make it one-party

Chosen over a runtime check or an analyzer: GoldenDbContext itself configures `Version` as a
concurrency token for every mapped `IAggregateRoot`. There is nothing to enforce because
forgetting is impossible by construction (repo standing directive: derive, don't verify).
"Behavior change for all hosts" (the 113 round-1 wontfix rationale) is the point — silent
lost updates become loud conflicts.

Implementation shape:

- **Model-finalizing convention**, not the `OnModelCreating` loop: it runs after all host
  configuration, so late `ApplyConfigurationsFromAssembly` calls cannot undo it, and
  config-only aggregates (no `DbSet` property) are covered — the current OnModelCreating pin
  has a latent gap there (base runs before ApplyConfigurationsFromAssembly; masked today
  because Profile has a DbSet). Move the existing `PropertyAccessMode.Property` pin into the
  same convention.
- **Sealed `ConfigureConventions` + new host virtual**: PostgresDbContext today overrides
  `ConfigureConventions` WITHOUT calling base — the exact forget-base trap we are deleting.
  GoldenDbContext seals `ConfigureConventions` (registers the golden convention, then calls a
  new `protected virtual OnConfigureConventions(ModelConfigurationBuilder)`); hosts override
  the virtual. Base call cannot be forgotten by construction.
- Scope: `IAggregateRoot` types only. Identity Principal/Credential are NOT roots — their
  manual `.IsConcurrencyToken()` (store-CAS belt, 104-032) is untouched.
- Profile's explicit `.IsConcurrencyToken()` becomes redundant — remove it; the how-to
  teaches "Version concurrency is golden, you get it free."
- No opt-out escape hatch yet: wanting one is treated as a design smell until a real case
  appears.

## Checklist

- [x] Decide enforcement point with Steve → auto-apply via model-finalizing convention
- [x] Golden convention: IsConcurrencyToken + PropertyAccessMode pin for IAggregateRoot Version
- [x] Seal ConfigureConventions; hosts move to OnConfigureConventions (PostgresDbContext updated)
- [x] Remove Profile's explicit IsConcurrencyToken; reconcile Design regions both files
- [x] Tests: root WITHOUT explicit mapping gets token+pin; config-only root (no DbSet) gets it too;
      Profile mapping test still asserts token (now from convention); identity mapping unaffected
- [x] Update ADR-0009 + HowToAddYourAggregate: two-party → one-party for IAggregateRoot
- [x] dev build 0/0, foundation-infrastructure-tests, web-infrastructure-tests,
      web-server-integration-tests, dev template-smoke — all green

## Notes

Origin: task 113 review round 1, finding M2 (wontfix-with-follow-on); filed during the
round-2 independent review. Files:
`source/foundation/foundation-infrastructure/persistence/golden-db-context.cs`,
`source/container-apps/web/web-infrastructure/persistence/postgres-db-context.cs`,
`source/container-apps/web/features/profile/profile-entity-type-configuration-infrastructure.cs`.
The convention ships in TimeWarp.Foundation.Infrastructure, so generated apps get it with no
template edit.

## Results (2026-07-24)

Implemented by build agent (commit 7e196656), independently reviewed and re-verified by the
orchestrator:

- `GoldenAggregateVersionConvention` (IModelFinalizingConvention, foundation-infrastructure):
  IsConcurrencyToken + PropertyAccessMode.Property on every mapped IAggregateRoot's Version —
  one-party contract, nothing for hosts to remember.
- `GoldenDbContext.ConfigureConventions` sealed (always registers the convention) + new
  `OnConfigureConventions` virtual; PostgresDbContext moved to the virtual. The old
  OnModelCreating pin loop removed — the convention subsumes it and closes its latent
  config-only/no-DbSet ordering gap (both proven by new tests).
- Profile's explicit `.IsConcurrencyToken()` removed; identity Principal/Credential
  (store-CAS, non-root) untouched. ADR-0009 + HowToAddYourAggregate updated to one-party
  language.
- Review note: EF configuration-source precedence means an explicit host
  `IsConcurrencyToken(false)` still beats the convention — a principled escape hatch exists
  by EF semantics without us shipping one.

Verification (re-run independently, not taken from the build agent's report): dev build 0/0;
foundation-infrastructure-tests 11/11 (incl. the two new convention proofs);
web-infrastructure-tests 39/39 (live Postgres); web-server-integration-tests 97/1;
dev template-smoke SUCCEEDED (SmokeDefault + SmokeNoPostgres).
