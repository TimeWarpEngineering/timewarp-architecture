#region Purpose
// Base type for domain entities: typed identity, identity-based equality, and a store-owned
// concurrency token every aggregate can rely on.
#endregion

#region Design
// TId is a [TypedId] value type (never a raw Guid) so entities from different aggregates can
// never be confused by id alone. Id is get-only — EF Core binds it via constructor-parameter
// name matching, so no setter exists for application code to reassign an entity's identity.
// Equality is exact runtime type + Id: two entities of different derived types sharing an Id
// value are never equal (the same exact-type discipline records/structs give structural
// equality for free, applied here to identity instead).
// Version is a store-owned optimistic-concurrency token that closes the 104-002 RFC D6
// last-write-wins debt uniformly for app entities. Hosts map it with .IsConcurrencyToken();
// application code never writes it — the store increments it on save.
#endregion

namespace TimeWarp.Foundation.Entities;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
  where TId : struct, IEquatable<TId>
{
  protected Entity(TId id)
  {
    Id = id;
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
