# Map Profile on PostgresDbContext with concurrency and Postgres integration tests

## Description

Child of [[113-golden-persistence-implementation-postgres-first-aspire-wired-with-actor-model-evaluation]].
Ship the template's **reference aggregate** end-to-end on the golden EF path: `Profile` (already
`IAggregateRoot` + private `Invariants` in web-domain). Proves entity config, concurrency-token
mapping (two-party Version contract), EnsureCreated schema, and real Postgres round-trips.

Depends on [[113-003-extract-goldendbcontext-to-foundation-and-fix-child-entity-savechanges-gap]]
for the golden base (can land after or with shared worktree if coordinated).

## Requirements

- `IEntityTypeConfiguration<Profile>`: table/schema (`profiles`), TypedId conversion,
  `.IsConcurrencyToken()` on Version, property access mode
- `DbSet<Profile>` on PostgresDbContext; ApplyConfigurationsFromAssembly (if not already)
- Prefer one thin write path so the model is not "config only" (tests may write via DbContext;
  optional GetProfile product wire if small — not blocking)
- Integration tests against real Postgres when connection available (Aspire testing builder /
  ephemeral volume — never shared WithDataVolume from 113-001 WAL lesson):
  - create → reload
  - concurrent update → DbUpdateConcurrencyException (or mapped conflict)
- No-postgres skip-mode unchanged for direct-host integration tests
- Design regions reconciled (TWA0004)
- `dev build` 0/0; relevant tests green

## Checklist

- [x] Profile EF configuration + DbSet
- [x] EnsureCreated creates Profile table
- [x] Postgres round-trip + concurrency tests
- [x] Skip-mode preserved without connection
- [x] Design regions
- [x] `dev build` 0/0 + tests green

## Notes

- Schema story: EnsureCreated remains template default; migrations documented for grown apps
  (113-005).
- Credit ledger / Orleans example is NOT this task.
- Identity EF is 104-032 after parent closeout.

## Session

- Created: 2026-07-23 (from 113 remaining-work plan)
- Implemented: 2026-07-23 — Profile mapped end-to-end; Postgres integration tests via Testcontainers
  (env connection override; silent skip when neither available). GetProfile product wire left TODO.
