#region Purpose
// Application port for Role persistence: handlers load, mutate, and save the Role aggregate through it.
#endregion

#region Design
// IProfileStore shape (task 473): Find/FindMany/List for reads, Add/Update/Remove for writes, and
// each write saves inside the call (no separate SaveChanges, so there is one save path and
// AggregateDbContext's invariants and Version hook always run).
//   - InMemoryRoleStore singleton is the zero-infra default (InMemoryRoleStoresModule), seeded
//     with the four product roles and RolePermissionSeed grants.
//   - EfRoleStore scoped replaces it when PostgresDbModule sees a connection string.
// Reads hand back snapshots: mutating a returned Role changes nothing until UpdateAsync.
// Add throws on a duplicate id. Update and Remove throw when the id is unknown. Remove calls
// Role.EnsureRemovable, so a product role is never deleted whatever the caller checked.
// List is the full set, small by construction (four product roles plus admin-created ones);
// GetRoles filters and orders it in the handler.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Features.Admin.Roles.Domain;

/// <summary>Durable Role lookup and writes (application port).</summary>
public interface IRoleStore
{
  /// <summary>Returns the role or null when no row exists for <paramref name="id"/>.</summary>
  Task<Role?> FindAsync(RoleId id, CancellationToken cancellationToken = default);

  /// <summary>Returns the roles that exist for <paramref name="ids"/>; missing ids are absent.</summary>
  Task<IReadOnlyList<Role>> FindManyAsync(IReadOnlyCollection<RoleId> ids, CancellationToken cancellationToken = default);

  /// <summary>Every stored role, in no particular order.</summary>
  Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default);

  /// <summary>Inserts a new role. Throws <see cref="InvalidOperationException"/> when the id already exists.</summary>
  Task AddAsync(Role role, CancellationToken cancellationToken = default);

  /// <summary>
  /// Persists mutations already applied on <paramref name="role"/> (details and both child sets).
  /// Throws <see cref="InvalidOperationException"/> when the id is unknown.
  /// </summary>
  Task UpdateAsync(Role role, CancellationToken cancellationToken = default);

  /// <summary>
  /// Deletes the role and its grants. Throws <see cref="InvalidOperationException"/> when the id is
  /// unknown or the role is a product role.
  /// </summary>
  Task RemoveAsync(RoleId id, CancellationToken cancellationToken = default);
}
