#region Purpose
// In-memory role → agent-scope grant store (no seeded scopes).
#endregion

#region Design
// Task 466: mirrors InMemoryRolePermissionStore. Product roles start with an empty agent-scope
// list, so a new agent whose effective role is Member may request only identity:read.
// Singleton process lifetime; PostgresDbModule swaps to the EF store when connected.
#endregion

namespace TimeWarp.Architecture.Authorization;

using System.Collections.Concurrent;

/// <summary>In-memory role → agent-scope grant store.</summary>
public sealed class InMemoryRoleAgentScopeStore : IRoleAgentScopeStore
{
  private readonly ConcurrentDictionary<Guid, string[]> Grants = new();

  public Task<IReadOnlyList<string>> GetScopesForRoleAsync(
    Guid roleId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    if (!Grants.TryGetValue(roleId, out string[]? scopes))
    {
      return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    return Task.FromResult<IReadOnlyList<string>>(scopes.ToArray());
  }

  public Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetScopesForRolesAsync(
    IReadOnlyCollection<Guid> roleIds,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(roleIds);
    cancellationToken.ThrowIfCancellationRequested();

    Dictionary<Guid, IReadOnlyList<string>> found = [];
    foreach (Guid roleId in roleIds.Distinct())
    {
      if (Grants.TryGetValue(roleId, out string[]? scopes))
      {
        found[roleId] = scopes.ToArray();
      }
    }

    return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>(found);
  }

  public Task SetScopesForRoleAsync(
    Guid roleId,
    IReadOnlyList<string> scopes,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(scopes);
    cancellationToken.ThrowIfCancellationRequested();

    string[] snapshot = scopes.Distinct(StringComparer.Ordinal).ToArray();
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
