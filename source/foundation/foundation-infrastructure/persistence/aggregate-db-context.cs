#region Purpose
// Abstract EF Core base that enforces the aggregate SaveChanges contract for every host context.
#endregion

#region Design
// Hosts (PostgresDbContext today; future api/grpc or spike contexts) inherit this type so the
// aggregate enforcement seam is not sealed inside a single product DbContext. Npgsql (and any other
// provider) stays host-only — this package references Microsoft.EntityFrameworkCore only.
// SaveChanges(Async) enforcement, two responsibilities per resolved Added/Modified IAggregateRoot,
// both before the base save executes:
//   1. DomainInvariantsGuard.EnsureValid — invalid aggregate state can never reach the store
//      regardless of which code path mutated it.
//   2. For Modified entries only: increment Entity{TId}.Version via the change-tracker's
//      PropertyEntry (EntityVersion.Next). This is the mechanism that makes the store-owned
//      concurrency token move; a private setter alone is never written by anything on its own.
// Child → root resolution closes the gap where mutating only a child/owned entity left the root
// Unchanged (so invariants + Version were skipped). Prefer FindOwnership() / non-ownership FKs:
// resolve the principal via DependentToPrincipal.TargetEntry when present; when the host maps
// OwnsMany/OwnsOne with WithOwner() and no CLR back-reference (DependentToPrincipal is null — the
// common template shape), match the FK values to a tracked entry of the principal entity type.
// Last resort: reference navigations targeting an IAggregateRoot. When the root is still Unchanged
// after a child change, mark it Modified so the concurrency token participates in the UPDATE.
// Concurrency-check wiring is now a one-party contract (task 121, 113 review M2 follow-on):
// ConfigureConventions is sealed here and always registers AggregateVersionConvention
// (same namespace, TimeWarp.Foundation.Persistence) before deferring to the new virtual
// OnConfigureConventions hook. That convention is an IModelFinalizingConvention — it runs after
// ALL host model configuration (OnModelCreating, ApplyConfigurationsFromAssembly, config-only
// aggregates with no DbSet property) and configures IsConcurrencyToken + PropertyAccessMode.Property
// on every mapped IAggregateRoot's Version property unconditionally. Forgetting the WHERE-clause
// half is no longer possible: hosts cannot reach ConfigureConventions to skip the registration —
// they customize their own conventions (e.g. PostgresDbContext's ConfigureTypedIdConventions) by
// overriding OnConfigureConventions instead, mirroring the same forget-base trap this replaces.
// Deleted aggregate roots are skipped — invariants and the concurrency token describe valid live
// state, not deletion. A deleted *child* still resolves to its live root so the root's Version
// moves with the aggregate change.
// IncrementModifiedVersions fails CLOSED and LOUD: IAggregateRoot is a standalone marker (nothing
// forces a concrete aggregate to also inherit Entity{TId}), so a Modified entry whose mapped model
// has no "Version" property, or one not typed long, throws a convention-pointing
// InvalidOperationException instead of letting EF's generic property-not-found message surface —
// mirrors DomainInvariantsGuard's fail-closed philosophy. AggregateVersionConvention only
// configures properties that already exist (skipping Version-less types there is harmless); the
// save path is the enforcement point, so silently skipping the increment would let a misdeclared
// root persist without ever moving its concurrency token.
#endregion

namespace TimeWarp.Foundation.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using TimeWarp.Foundation.Application.Services;
using TimeWarp.Foundation.Entities;

public abstract class AggregateDbContext : DbContext
{
  internal const string VersionPropertyName = nameof(Entity<Guid>.Version);

  protected AggregateDbContext(DbContextOptions options) : base(options) { }

  public override int SaveChanges(bool acceptAllChangesOnSuccess)
  {
    List<EntityEntry> aggregateRootEntries = ChangedAggregateRootEntries();
    DomainInvariantsGuard.EnsureValid(aggregateRootEntries.Select(entry => entry.Entity));
    IncrementModifiedVersions(aggregateRootEntries);
    return base.SaveChanges(acceptAllChangesOnSuccess);
  }

  public override Task<int> SaveChangesAsync
  (
    bool acceptAllChangesOnSuccess,
    CancellationToken cancellationToken = default
  )
  {
    List<EntityEntry> aggregateRootEntries = ChangedAggregateRootEntries();
    DomainInvariantsGuard.EnsureValid(aggregateRootEntries.Select(entry => entry.Entity));
    IncrementModifiedVersions(aggregateRootEntries);
    return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
  }

  protected sealed override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
  {
    configurationBuilder.Conventions.Add(_ => new AggregateVersionConvention());
    OnConfigureConventions(configurationBuilder);
  }

  /// <summary>
  /// Hosts override this instead of <see cref="ConfigureConventions"/> (sealed here) to register
  /// their own conventions — e.g. PostgresDbContext's ConfigureTypedIdConventions. The aggregate
  /// Version concurrency convention is always registered first and cannot be skipped.
  /// </summary>
  protected virtual void OnConfigureConventions(ModelConfigurationBuilder configurationBuilder)
  {
  }

  private List<EntityEntry> ChangedAggregateRootEntries()
  {
    // Materialize before any State writes — setting a root to Modified mutates the change-tracker
    // map and would invalidate a live Entries() enumerator.
    var trackedEntries = ChangeTracker.Entries().ToList();
    Dictionary<object, EntityEntry> roots = new(ReferenceEqualityComparer.Instance);

    foreach (EntityEntry entry in trackedEntries)
    {
      if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
      {
        continue;
      }

      EntityEntry? rootEntry = ResolveAggregateRootEntry(entry);
      if (rootEntry is null)
      {
        continue;
      }

      // Deleted roots: skip — invariants / Version describe live state only.
      if (rootEntry.State == EntityState.Deleted)
      {
        continue;
      }

      // Child-only mutation leaves the root Unchanged; mark Modified so Version + concurrency run.
      if (rootEntry.State == EntityState.Unchanged)
      {
        rootEntry.State = EntityState.Modified;
      }

      if (rootEntry.State is EntityState.Added or EntityState.Modified)
      {
        roots.TryAdd(rootEntry.Entity, rootEntry);
      }
    }

    return roots.Values.ToList();
  }

  private static EntityEntry? ResolveAggregateRootEntry(EntityEntry entry)
  {
    EntityEntry current = entry;
    HashSet<object> visited = new(ReferenceEqualityComparer.Instance);

    while (true)
    {
      if (!visited.Add(current.Entity))
      {
        return null;
      }

      if (current.Entity is IAggregateRoot)
      {
        return current;
      }

      EntityEntry? owner = TryResolveOwnerEntry(current);
      if (owner is null)
      {
        return null;
      }

      current = owner;
    }
  }

  private static EntityEntry? TryResolveOwnerEntry(EntityEntry entry)
  {
    // Preferred path: EF ownership metadata (OwnsOne / OwnsMany).
    IForeignKey? ownership = entry.Metadata.FindOwnership();
    if (ownership is not null)
    {
      EntityEntry? owner = ResolvePrincipalEntry(entry, ownership);
      if (owner is not null)
      {
        return owner;
      }
    }

    // Non-owned children: walk FK principal end when present.
    foreach (IForeignKey foreignKey in entry.Metadata.GetForeignKeys())
    {
      if (foreignKey.IsOwnership)
      {
        continue;
      }

      EntityEntry? principal = ResolvePrincipalEntry(entry, foreignKey);
      if (principal is not null)
      {
        return principal;
      }
    }

    // Last resort: a reference navigation already pointing at a tracked IAggregateRoot.
    foreach (INavigation navigation in entry.Metadata.GetNavigations())
    {
      if (navigation.IsCollection || !navigation.IsOnDependent)
      {
        continue;
      }

      EntityEntry? target = entry.Reference(navigation.Name).TargetEntry;
      if (target?.Entity is IAggregateRoot)
      {
        return target;
      }
    }

    return null;
  }

  private static EntityEntry? ResolvePrincipalEntry(EntityEntry dependent, IForeignKey foreignKey)
  {
    INavigation? toPrincipal = foreignKey.DependentToPrincipal;
    if (toPrincipal is not null)
    {
      EntityEntry? viaNavigation = dependent.Reference(toPrincipal.Name).TargetEntry;
      if (viaNavigation is not null)
      {
        return viaNavigation;
      }
    }

    // OwnsMany/OwnsOne mapped with WithOwner() and no CLR back-reference leave
    // DependentToPrincipal null. Match principal key values among tracked entries.
    IReadOnlyList<IProperty> foreignKeyProperties = foreignKey.Properties;
    IReadOnlyList<IProperty> principalKeyProperties = foreignKey.PrincipalKey.Properties;
    if (foreignKeyProperties.Count != principalKeyProperties.Count || foreignKeyProperties.Count == 0)
    {
      return null;
    }

    object[] foreignKeyValues = new object[foreignKeyProperties.Count];
    for (int index = 0; index < foreignKeyProperties.Count; index++)
    {
      PropertyEntry property = dependent.Property(foreignKeyProperties[index].Name);
      object? value = property.CurrentValue ?? property.OriginalValue;
      if (value is null)
      {
        return null;
      }

      foreignKeyValues[index] = value;
    }

    Type principalClrType = foreignKey.PrincipalEntityType.ClrType;
    foreach (EntityEntry candidate in dependent.Context.ChangeTracker.Entries())
    {
      if (!principalClrType.IsInstanceOfType(candidate.Entity)
          && candidate.Metadata != foreignKey.PrincipalEntityType)
      {
        continue;
      }

      bool matches = true;
      for (int index = 0; index < principalKeyProperties.Count; index++)
      {
        object? principalValue = candidate.Property(principalKeyProperties[index].Name).CurrentValue;
        if (!Equals(principalValue, foreignKeyValues[index]))
        {
          matches = false;
          break;
        }
      }

      if (matches)
      {
        return candidate;
      }
    }

    return null;
  }

  private static void IncrementModifiedVersions(IEnumerable<EntityEntry> entries)
  {
    foreach (EntityEntry entry in entries)
    {
      if (entry.State != EntityState.Modified)
      {
        continue;
      }

      IProperty? versionMetadata = entry.Metadata.FindProperty(VersionPropertyName);
      if (versionMetadata is null || versionMetadata.ClrType != typeof(long))
      {
        throw new InvalidOperationException
        (
          $"'{entry.Entity.GetType().Name}' implements IAggregateRoot but has no mapped 'long {VersionPropertyName}' property. " +
          "Aggregate roots must inherit Entity<TId> so Version is mapped by convention. See the Entity<TId> XML docs for a worked example."
        );
      }

      PropertyEntry versionProperty = entry.Property(VersionPropertyName);
      versionProperty.CurrentValue = EntityVersion.Next((long)versionProperty.OriginalValue!);
    }
  }
}
