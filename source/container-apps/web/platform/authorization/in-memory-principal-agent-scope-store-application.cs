#region Purpose
// In-memory principal → agent-scope grant store.
#endregion

#region Design
// Task 466: mirrors InMemoryPrincipalRoleStore's replace-set. Missing key and empty list are
// both "no direct grants". Singleton process lifetime; PostgresDbModule swaps to the EF store
// when connected.
#endregion

namespace TimeWarp.Architecture.Authorization;

using System.Collections.Concurrent;
using TimeWarp.Identity;

/// <summary>In-memory principal → agent-scope grant store.</summary>
public sealed class InMemoryPrincipalAgentScopeStore : IPrincipalAgentScopeStore
{
  private readonly ConcurrentDictionary<PrincipalId, string[]> Grants = new();

  public Task<IReadOnlyList<string>> GetScopesAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    if (!Grants.TryGetValue(principalId, out string[]? scopes))
    {
      return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    return Task.FromResult<IReadOnlyList<string>>(scopes.ToArray());
  }

  public Task SetScopesAsync(
    PrincipalId principalId,
    IReadOnlyList<string> scopes,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(scopes);
    cancellationToken.ThrowIfCancellationRequested();

    string[] snapshot = scopes.Distinct(StringComparer.Ordinal).ToArray();
    if (snapshot.Length == 0)
    {
      Grants.TryRemove(principalId, out _);
    }
    else
    {
      Grants[principalId] = snapshot;
    }

    return Task.CompletedTask;
  }
}
