#region Purpose
// EF Core state-store for the Ledger aggregate. Maps the aggregate, pins Version as the
// optimistic-concurrency token, and replicates the golden SaveChanges enforcement hook.
#endregion

#region Design
// DOCUMENTED DUPLICATION (the seam-packaging finding for 113 decision 5): the template's shipped
// PostgresDbContext is `sealed`, so this spike cannot subclass it to inherit the ~40-line
// SaveChanges hook — it must replicate the hook here. The replication calls the SAME foundation
// seams (DomainInvariantsGuard.EnsureValid, EntityVersion.Next) via ProjectReference, so only the
// glue is copied, not the logic. If this hook and the golden one drift, that is exactly the risk a
// shared non-sealed base (or a DbContext-agnostic SaveChanges interceptor packaged in foundation)
// would remove — recorded for the 113 write-up.
// Unlike the entity-free PostgresDbContext, this context maps a concrete aggregate (Ledger), so it
// completes BOTH halves of the concurrency mechanism the golden Entity<TId> design calls out:
//   1. the hook increments Version on every Modified IAggregateRoot save (via PropertyEntry), and
//   2. OnModelCreating calls .IsConcurrencyToken() on Version, so EF emits `WHERE ... AND Version =
//      @original` — a stale writer's UPDATE matches zero rows and throws DbUpdateConcurrencyException.
// That pairing is what the baseline concurrency test provokes and the actor/grain tests avoid.
// PrincipalId <-> Guid conversion is configured here (the spike's PrincipalId is a plain record
// struct, not an EF-known primitive).
#endregion

namespace TimeWarp.Spike.DualActor;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using TimeWarp.Foundation.Application.Services;
using TimeWarp.Foundation.Entities;

public sealed class LedgerDbContext : DbContext
{
  private const string VersionPropertyName = nameof(Entity<Guid>.Version);

  public LedgerDbContext(DbContextOptions<LedgerDbContext> options) : base(options) { }

  public DbSet<Ledger> Ledgers => Set<Ledger>();

  public override int SaveChanges(bool acceptAllChangesOnSuccess)
  {
    List<EntityEntry> aggregateRootEntries = ChangedAggregateRootEntries();
    DomainInvariantsGuard.EnsureValid(aggregateRootEntries.Select(entry => entry.Entity));
    IncrementModifiedVersions(aggregateRootEntries);
    return base.SaveChanges(acceptAllChangesOnSuccess);
  }

  public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
  {
    List<EntityEntry> aggregateRootEntries = ChangedAggregateRootEntries();
    DomainInvariantsGuard.EnsureValid(aggregateRootEntries.Select(entry => entry.Entity));
    IncrementModifiedVersions(aggregateRootEntries);
    return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Ledger>(ledger =>
    {
      ledger.ToTable("ledgers");
      ledger.HasKey(l => l.Id);
      ledger.Property(l => l.Id)
        .HasConversion(id => id.Value, value => new PrincipalId(value));
      ledger.Property(l => l.Balance);
      // Both halves of the concurrency mechanism: the hook increments Version, this makes EF
      // compare the original value in the UPDATE WHERE clause.
      ledger.Property(l => l.Version).IsConcurrencyToken();
    });

    // Pin PropertyAccessMode.Property for every mapped IAggregateRoot's Version, matching the
    // golden PostgresDbContext (defense-in-depth so the hook's PropertyEntry write is independent
    // of backing-field naming).
    foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes().ToList())
    {
      if (!typeof(IAggregateRoot).IsAssignableFrom(entityType.ClrType)) continue;
      if (entityType.ClrType.GetProperty(VersionPropertyName) is null) continue;

      modelBuilder.Entity(entityType.ClrType)
        .Property(VersionPropertyName)
        .UsePropertyAccessMode(PropertyAccessMode.Property);
    }
  }

  private List<EntityEntry> ChangedAggregateRootEntries() =>
    ChangeTracker.Entries()
      .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
      .Where(entry => entry.Entity is IAggregateRoot)
      .ToList();

  private static void IncrementModifiedVersions(IEnumerable<EntityEntry> entries)
  {
    foreach (EntityEntry entry in entries)
    {
      if (entry.State != EntityState.Modified) continue;

      IProperty? versionMetadata = entry.Metadata.FindProperty(VersionPropertyName);
      if (versionMetadata is null || versionMetadata.ClrType != typeof(long))
      {
        throw new InvalidOperationException(
          $"'{entry.Entity.GetType().Name}' implements IAggregateRoot but has no mapped 'long {VersionPropertyName}' property.");
      }

      PropertyEntry versionProperty = entry.Property(VersionPropertyName);
      versionProperty.CurrentValue = EntityVersion.Next((long)versionProperty.OriginalValue!);
    }
  }
}
