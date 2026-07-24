# Aggregates

An aggregate is a domain entity that is the consistency boundary for a set of invariants.
Every aggregate root in this project follows the same golden pattern (task 106), mirrored from
the identity library (`source/libraries/timewarp-identity`, Principal/Credential):

- **Typed id.** The aggregate's id is a `[TypedId] readonly partial record struct` (e.g.
  `ProfileId`), never a raw `Guid`. See `profile/profile-id.cs`.
- **`Entity<TId>` base.** The aggregate inherits `TimeWarp.Foundation.Entities.Entity<TId>`
  (get-only typed `Id`, identity-based equality, a store-owned `Version` concurrency token) and
  implements the marker interface `IAggregateRoot`.
- **Fail-closed construction.** A private constructor plus a static `Create(...)` factory with
  guard clauses — an aggregate can never exist half-initialized or with an obviously-invalid
  required field.
- **Named mutations, no public setters.** State changes are intention-revealing methods
  (`Rename`, `SetLanguage`, ...), never `{ get; set; }`.
- **Nested `Invariants` validator.** A `private sealed class Invariants : AbstractValidator<T>`
  declares the aggregate's full rule set. It stays `private` so contract-validator
  auto-registration (`AddValidatorsFromAssemblyContaining`) never picks it up as a request
  validator — enforced by `TWA0011` (must exist) and `TWA0012` (must be private).
- **Save-time enforcement.** `TimeWarp.Foundation.Application.Services.DomainInvariantsGuard`
  discovers and runs the nested `Invariants` validator for every changed `IAggregateRoot` from
  `AggregateDbContext.SaveChanges(Async)` (`foundation-infrastructure/persistence/aggregate-db-context.cs`)
  before the save proceeds. Host contexts such as `PostgresDbContext` inherit that base; they do
  not reimplement the hook. Guard clauses in `Create`/mutations and the save-time validator are
  complementary, not redundant: the former makes invalid states hard to construct, the latter
  makes them impossible to persist regardless of which code path produced them. Child-only
  mutations resolve to the owning root so invariants and `Version` still run.

`profile/profile.cs` is the exemplar — read it alongside this file before adding a new
aggregate. Its EF mapping (schema `profiles`, TypedId conversion) lives under
`web/features/profile/profile-entity-type-configuration-infrastructure.cs` and is applied by
`PostgresDbContext` (`ApplyConfigurationsFromAssembly`). `Version`'s `.IsConcurrencyToken()` is
supplied for free by `AggregateDbContext`'s Version convention — Profile's own mapping does not
declare it. End-to-end walkthrough
(domain → config → SaveChanges → tests, store ports, Orleans, EnsureCreated vs Migrate):
`documentation/developer/how-to-guides/HowToAddYourAggregate.md` (ADR-0009).
