#region Purpose
// EF Core context for the PostgreSQL store; the seam where template consumers add their entity sets.
#endregion

#region Design
// Deliberately entity-free: the template ships the Postgres plumbing (module, health and
// environment checks, schema-creation hosted service) and leaves the model to the consumer.
// Connection setup lives in PostgresDbModule.ConfigurePostgresDb, not OnConfiguring, so the
// context stays configuration-agnostic.
// SaveChanges(Async) enforcement hook: every Added/Modified entry whose Entity is an
// IAggregateRoot is run through DomainInvariantsGuard.EnsureValid before the base save executes,
// so invalid aggregate state can never reach the store regardless of which code path mutated it.
// Deleted entries are skipped by design — invariants describe valid live state, not deletion.
// SqlDbContext (sql-db-context.cs) is unregistered dead code today; a consumer who activates it
// should copy this same override.
#endregion

namespace TimeWarp.Architecture.Persistence;

using TimeWarp.Foundation.Application.Services;

public sealed partial class PostgresDbContext : DbContext
{
  public override int SaveChanges(bool acceptAllChangesOnSuccess)
  {
    DomainInvariantsGuard.EnsureValid(ChangedAggregateRoots());
    return base.SaveChanges(acceptAllChangesOnSuccess);
  }

  public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
  {
    DomainInvariantsGuard.EnsureValid(ChangedAggregateRoots());
    return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
  }

  private IEnumerable<object> ChangedAggregateRoots() =>
    ChangeTracker.Entries()
      .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
      .Select(entry => entry.Entity)
      .Where(entity => entity is IAggregateRoot);
}
