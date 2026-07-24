# Extract GoldenDbContext to Foundation and fix child-entity SaveChanges gap

## Description

Child of [[113-golden-persistence-implementation-postgres-first-aspire-wired-with-actor-model-evaluation]]
decision 5 + child-entity gap. Graduate the golden aggregate SaveChanges hook out of sealed
`PostgresDbContext` into an abstract `GoldenDbContext` in `TimeWarp.Foundation.Infrastructure`
so host contexts (web, future api/grpc, spikes) share one implementation. Fix the documented
gap where mutating only a child/owned entity leaves the root `Unchanged` (invariants + Version
increment skipped).

## Requirements

- Add `Microsoft.EntityFrameworkCore` to foundation-infrastructure (Npgsql stays host-only).
- Abstract unsealed `GoldenDbContext : DbContext` with:
  - SaveChanges / SaveChangesAsync hook: `DomainInvariantsGuard` + `EntityVersion.Next` on
    resolved aggregate roots
  - OnModelCreating base: pin PropertyAccessMode for Version on `IAggregateRoot` (if host
    currently does this)
  - `ChangedAggregateRootEntries()` including **child → root** resolution via ownership
    (`FindOwnership`) and/or parent navigations; mark root `Modified` when only children dirty
- `PostgresDbContext` inherits base; delete duplicated hook body
- Automated tests for child-only mutation (Version + invariants), root-only modify, and
  fail-closed missing Version
- Reconcile Design regions (entity.cs / postgres-db-context) that name PostgresDbContext as the
  sole host of the hook
- `dev build` 0/0

## Checklist

- [x] GoldenDbContext in foundation-infrastructure + EF package ref / CPM
- [x] Child→root resolution + root State=Modified when children dirty
- [x] PostgresDbContext thinned to host subclass
- [x] Hook + gap tests green
- [x] Design regions reconciled
- [x] `dev build` 0/0

## Notes

- Parent plan 2026-07-23: Steve soft-gate on EF-on-Foundation (silence = accept).
- Identity Principal/Credential are independent versioned entities — not the child-gap model;
  use a test-local Root + owned child for the gap test.
- Outbox / Profile mapping / ADR are sibling children 113-004 / 113-005.

## Results

**Implemented 2026-07-23** — `GoldenDbContext` graduated to Foundation; child-entity SaveChanges gap closed; host thinned.

### What was implemented
- Abstract unsealed `GoldenDbContext` in `TimeWarp.Foundation.Persistence` with SaveChanges/Async hook, child→root resolution (ownership / FK principal match / reference navigations), Version PropertyAccessMode pin, fail-closed missing Version
- `PostgresDbContext` inherits base; duplicated hook body removed
- EF Core package on foundation-infrastructure (Npgsql stays host-only)
- 7 Fixie tests in foundation-infrastructure-tests (InMemory harness)

### Files changed
- `source/foundation/foundation-infrastructure/persistence/golden-db-context.cs` (new)
- `source/foundation/foundation-infrastructure/foundation-infrastructure.csproj`
- `Directory.Packages.props` (EF Core + InMemory)
- `source/container-apps/web/web-infrastructure/persistence/postgres-db-context.cs`
- Design regions: entity.cs, entity-version.cs, domain-invariants-guard.cs
- `tests/foundation/foundation-infrastructure-tests/golden-db-context-tests.cs` (+ csproj/usings)

### Key decisions
- FK principal match required for `OwnsMany` + `WithOwner()` without CLR back-nav
- Materialize `ChangeTracker.Entries()` before marking roots Modified (live enum throws)
- No Profile/outbox/Orleans (siblings)

### Tests
- `dev build` 0/0
- `dotnet fixie tests/foundation/foundation-infrastructure-tests` — 7 passed

### Commit
- `a462f7bb` feat(foundation): extract GoldenDbContext and close child-entity SaveChanges gap

## Session

- Created: 2026-07-23 (from 113 remaining-work plan)
- Implementation: 2026-07-23 (build agent via orchestrator)
