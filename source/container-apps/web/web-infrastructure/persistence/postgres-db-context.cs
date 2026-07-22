#region Purpose
// EF Core context for the PostgreSQL store; the seam where template consumers add their entity sets.
#endregion

#region Design
// Deliberately entity-free: the template ships the Postgres plumbing (module, health and
// environment checks, schema-creation hosted service) and leaves the model to the consumer.
// Connection setup lives in PostgresDbModule.ConfigurePostgresDb, not OnConfiguring, so the
// context stays configuration-agnostic.
// SaveChanges(Async) enforcement hook, two responsibilities per changed Added/Modified
// IAggregateRoot entry, both before the base save executes:
//   1. DomainInvariantsGuard.EnsureValid — invalid aggregate state can never reach the store
//      regardless of which code path mutated it.
//   2. For Modified entries only: increment Entity<TId>.Version via the change-tracker's
//      PropertyEntry (EntityVersion.Next — a pure, unit-tested seam; see entity-version.cs /
//      entity-version-tests.cs). This is the REAL mechanism that makes the store-owned concurrency
//      token move; a private setter alone (as an earlier version of entity.cs's Design region
//      incorrectly claimed) is never written by anything on its own.
// Concurrency-check wiring is a two-party contract, not something this hook can complete alone:
// this hook always increments Version on Modified saves, but nothing compares the original value in
// an UPDATE's WHERE clause unless the host ALSO configures .IsConcurrencyToken() on the property
// when it maps its own concrete aggregate (OnModelCreating below cannot do that for entity types it
// does not know about yet — it only pins the access mode). Skipping that host-side step leaves the
// increment as silent bookkeeping with no enforcement — pair the two.
// OnModelCreating pins PropertyAccessMode.Property for every mapped IAggregateRoot's "Version"
// property. EF Core's default access mode (PreferFieldDuringConstruction) already routes
// non-construction writes — including the PropertyEntry.CurrentValue set this hook performs —
// through the property, and EF's compiled accessors invoke a private setter via reflection
// regardless of its declared C# accessibility, so this pin is defense-in-depth documentation of
// that reliance rather than a behavior change; it also makes the hook's write independent of
// whatever backing-field naming a consumer's compiled entity happens to use.
// Deleted entries are skipped by design — invariants (and the concurrency token) describe valid
// live state, not deletion. This is the sole persistence context the template ships.
// Known boundary: only entries whose OWN Entity is IAggregateRoot are considered. A save that
// mutates only a child/owned entity leaves its owning root's entry Unchanged, so that root's
// invariants and Version increment do not run for that save. Resolving changed child entries to
// their owning aggregate root via navigation metadata is the intended future fix once a real entity
// model with child/owned types exists in this template; today Profile is single-entity, so the gap
// is latent, not exercised.
// IncrementModifiedVersions fails CLOSED and LOUD: IAggregateRoot is a standalone marker interface
// (nothing forces a concrete aggregate to also inherit Entity<TId>), so a Modified entry whose
// mapped model has no "Version" property, or one not typed long, throws a convention-pointing
// InvalidOperationException instead of letting EF's generic property-not-found message or an
// InvalidCastException surface — this mirrors DomainInvariantsGuard's own fail-closed philosophy
// (a defect here is exactly as serious as a missing Invariants validator). This is deliberately NOT
// a skip-and-continue like OnModelCreating's guard above: OnModelCreating only decides whether to
// pin the access mode for properties that already exist, so skipping a Version-less type there is
// harmless (there is nothing to pin); the save path is the actual enforcement point, so silently
// skipping the increment there would let a misdeclared aggregate root persist without ever moving
// its concurrency token. No unit test covers this throw path — like the increment itself (see
// entity-version.cs), it requires a live change-tracker EntityEntry/PropertyEntry that this
// entity-free context has no DbSet to produce without a new EF test package (InMemory/Sqlite),
// which is out of scope here.
#endregion

namespace TimeWarp.Architecture.Persistence;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using TimeWarp.Foundation.Application.Services;

public sealed partial class PostgresDbContext : DbContext
{
  private const string VersionPropertyName = nameof(Entity<Guid>.Version);

  public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options) { }

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
          $"'{entry.Entity.GetType().Name}' implements IAggregateRoot but has no mapped 'long {VersionPropertyName}' property. " +
          "Aggregate roots must inherit Entity<TId> (source/foundation/foundation-domain/entities/base/entity.cs) so Version is " +
          "mapped by convention — see the golden aggregate pattern exemplar (web-domain/aggregates/profile/profile.cs and " +
          "aggregates/overview.md).");
      }

      PropertyEntry versionProperty = entry.Property(VersionPropertyName);
      versionProperty.CurrentValue = EntityVersion.Next((long)versionProperty.OriginalValue!);
    }
  }
}
