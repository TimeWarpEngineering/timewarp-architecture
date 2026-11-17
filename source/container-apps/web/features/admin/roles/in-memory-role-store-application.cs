#region Purpose
// Thread-safe in-memory IRoleStore for zero-infra / skip-mode (default without Postgres).
#endregion

#region Design
// Task 473: ConcurrentDictionary keyed by RoleId, process-lifetime singleton like
// InMemoryProfileStore. The constructor seeds the four product roles from ProductRoleSeed with
// RolePermissionSeed.DefaultGrants permissions and no agent scopes, the same starting state the
// AddRoles migration gives Postgres.
// Snapshot semantics: Add and Update store a copy, and every read returns a copy, so a handler's
// mutation is invisible to other readers until UpdateAsync, as with the EF store's AsNoTracking
// reads. The copy is rebuilt through Role's public API (Create with the fixed id, then the two
// full-replace setters), so a snapshot can never hold state the aggregate would refuse. Version is
// not tracked here (no change tracker); concurrent edits of one role are a Postgres concern.
// The authorization read ports in skip-mode (InMemoryRolePermissionStore,
// InMemoryRoleAgentScopeStore) read this store, so a SetRolePermissions write is what the
// permission evaluator sees next.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using System.Collections.Concurrent;
using TimeWarp.Architecture.Authorization;
using TimeWarp.Architecture.Features.Admin.Roles.Domain;

/// <summary>In-memory Role store (zero-infra default), seeded with the product roles.</summary>
public sealed class InMemoryRoleStore : IRoleStore
{
  private readonly ConcurrentDictionary<RoleId, Role> Roles = new();

  public InMemoryRoleStore()
  {
    foreach (ProductRoleSeed.Entry seed in ProductRoleSeed.Roles)
    {
      var role = Role.Create(RoleId.From(seed.Id), seed.Name, seed.Description);
      if (RolePermissionSeed.DefaultGrants.TryGetValue(seed.Id, out IReadOnlyList<string>? permissionIds))
      {
        role.SetPermissions(permissionIds);
      }

      Roles[role.Id] = role;
    }
  }

  public Task<Role?> FindAsync(RoleId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(Roles.TryGetValue(id, out Role? role) ? Copy(role) : null);
  }

  public Task<IReadOnlyList<Role>> FindManyAsync(
    IReadOnlyCollection<RoleId> ids,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(ids);
    cancellationToken.ThrowIfCancellationRequested();
    List<Role> found = [];
    foreach (RoleId id in ids.Distinct())
    {
      if (Roles.TryGetValue(id, out Role? role))
      {
        found.Add(Copy(role));
      }
    }

    return Task.FromResult<IReadOnlyList<Role>>(found);
  }

  public Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IReadOnlyList<Role> all = [.. Roles.Values.Select(Copy)];
    return Task.FromResult(all);
  }

  public Task AddAsync(Role role, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(role);
    cancellationToken.ThrowIfCancellationRequested();

    if (!Roles.TryAdd(role.Id, Copy(role)))
    {
      throw new InvalidOperationException($"Role '{role.Id}' already exists.");
    }

    return Task.CompletedTask;
  }

  public Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(role);
    cancellationToken.ThrowIfCancellationRequested();

    Role snapshot = Copy(role);
    if (!Roles.TryGetValue(role.Id, out Role? current) || !Roles.TryUpdate(role.Id, snapshot, current))
    {
      throw new InvalidOperationException($"Role '{role.Id}' does not exist.");
    }

    return Task.CompletedTask;
  }

  public Task RemoveAsync(RoleId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!Roles.TryGetValue(id, out Role? current))
    {
      throw new InvalidOperationException($"Role '{id}' does not exist.");
    }

    current.EnsureRemovable();
    Roles.TryRemove(id, out _);
    return Task.CompletedTask;
  }

  private static Role Copy(Role role)
  {
    var copy = Role.Create(role.Id, role.Name, role.Description);
    copy.SetPermissions(role.Permissions.Select(permission => permission.PermissionId));
    copy.SetAgentScopes(role.AgentScopes.Select(scope => scope.Scope));
    return copy;
  }
}
