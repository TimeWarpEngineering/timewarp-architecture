#region Purpose
// Base type for domain entities: typed identity, identity-based equality, and a store-owned
// concurrency token every aggregate can rely on.
#endregion

#region Design
// By convention TId is a [TypedId] value type (never a raw Guid) so entities from different
// aggregates cannot be confused by id alone — the `where TId : struct, IEquatable<TId>` constraint
// does not enforce this (Entity<Guid> compiles, and entity-tests.cs deliberately instantiates it to
// exercise equality without needing a real TypedId); a build-time analyzer checking for a raw Guid
// TId is a candidate future enforcement (prefer-analyzers directive), not yet built. Id is get-only
// — EF Core binds it via constructor-parameter name matching, so no setter exists for application
// code to reassign an entity's identity.
// Equality is exact runtime type + Id: two entities of different derived types sharing an Id
// value are never equal (the same exact-type discipline records/structs give structural
// equality for free, applied here to identity instead).
// Version is a store-owned optimistic-concurrency token that closes the 104-002 RFC D6
// last-write-wins debt — but ONLY when both halves of the mechanism are present, and this type
// alone is not one of them:
//   1. The real increment lives in GoldenDbContext.SaveChanges(Async)
//      (TimeWarp.Foundation.Persistence), which sets EntityVersion.Next(originalVersion) onto the
//      change-tracker's PropertyEntry.CurrentValue for the "Version" property of every Modified
//      IAggregateRoot (including roots resolved from dirty children/owned entities) — this bypasses
//      the private setter the same way EF already writes any other private-setter property (its
//      compiled accessors invoke the setter via reflection regardless of C# accessibility); nothing
//      in Entity<TId> itself, and no attribute on this property, increments anything. Host contexts
//      (e.g. PostgresDbContext) inherit GoldenDbContext rather than reimplementing the hook.
//   2. Hosts that map a concrete aggregate MUST additionally call .IsConcurrencyToken() on the
//      "Version" property in their own OnModelCreating (GoldenDbContext / PostgresDbContext cannot
//      do this for entity types they do not know about) — without that, the hook still increments
//      Version on every save, but nothing compares the original value in the UPDATE's WHERE clause,
//      so a stale overwrite still proceeds silently. The increment and the concurrency check are a
//      pair; shipping only one of them leaves the D6 debt open.
// application code never writes Version directly; the private setter exists only so EF's
// reflection-based property access can reach it.
// Non-EF stores (e.g. timewarp-identity's in-memory IPrincipalStore, task 104-028) have no
// change-tracker to write through, so they need a different seam: `protected Entity(TId id, long
// version)` is construction-time-only rehydration — a store "increments" by constructing a brand
// new instance with `EntityVersion.Next(storedVersion)`, not by mutating an existing one. There is
// no bump method and no mutator; the two-ctor shape (id-only delegates to id+version with 0) keeps
// every existing EF-mapped subclass (e.g. Profile) unchanged — it still calls `base(id)`, still
// gets Version 0, and EF still owns every subsequent write via the mechanism above.
#endregion

namespace TimeWarp.Foundation.Entities;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
  where TId : struct, IEquatable<TId>
{
  protected Entity(TId id) : this(id, 0)
  {
  }

  protected Entity(TId id, long version)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(version);
    Id = id;
    Version = version;
  }

  public TId Id { get; }

  public long Version { get; private set; }

  public bool Equals(Entity<TId>? other)
  {
    if (other is null) return false;
    if (ReferenceEquals(this, other)) return true;
    return GetType() == other.GetType() && Id.Equals(other.Id);
  }

  public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

  public override int GetHashCode() => HashCode.Combine(GetType(), Id);

  public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
    left is null ? right is null : left.Equals(right);

  public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
