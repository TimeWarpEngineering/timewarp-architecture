#region Purpose
// Thread-safe in-memory IRolePermissionStore seeded from RolePermissionSeed (zero-infra default).
#endregion

#region Design
// Task 182-001: ConcurrentDictionary keyed by RoleId; constructor copies RolePermissionSeed.DefaultGrants
// so greenfield / skip-mode matches EF migration seed. Set replaces the full list (snapshot);
// empty list removes the key. Missing key → empty Get (same as principal-role store). Singleton
// process lifetime matches InMemoryPrincipalRoleStore; PostgresDbModule swaps to scoped
// EfRolePermissionStore when a connection string is present. Features substrate namespace —
// see IRolePermissionStore Design.
#endregion

namespace TimeWarp.Architecture.Features;

using System.Collections.Concurrent;

/// <summary>In-memory role → permission grant store (seeded defaults).</summary>
public sealed class InMemoryRolePermissionStore : IRolePermissionStore
{
  private readonly ConcurrentDictionary<Guid, string[]> Grants = new();

  public InMemoryRolePermissionStore()
  {
    foreach ((Guid roleId, IReadOnlyList<string> permissionIds) in RolePermissionSeed.DefaultGrants)
    {
      Grants[roleId] = permissionIds.Distinct(StringComparer.Ordinal).ToArray();
    }
  }

  public Task<IReadOnlyList<string>> GetPermissionIdsForRoleAsync(
    Guid roleId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    if (!Grants.TryGetValue(roleId, out string[]? permissions))
    {
      return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    return Task.FromResult<IReadOnlyList<string>>(permissions.ToArray());
  }

  public Task SetPermissionIdsForRoleAsync(
    Guid roleId,
    IReadOnlyList<string> permissionIds,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(permissionIds);
    cancellationToken.ThrowIfCancellationRequested();

    string[] snapshot = permissionIds.Distinct(StringComparer.Ordinal).ToArray();
    if (snapshot.Length == 0)
    {
      Grants.TryRemove(roleId, out _);
    }
    else
    {
      Grants[roleId] = snapshot;
    }

    return Task.CompletedTask;
  }
}
