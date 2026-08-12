#region Purpose
// Thread-safe in-memory IPrincipalRoleStore for zero-infra / skip-mode (default without Postgres).
#endregion

#region Design
// Task 147-004: ConcurrentDictionary keyed by PrincipalId; Set replaces the full list (snapshot).
// Missing key and empty list are both "no stored roles" — Get returns empty array so the
// effective-roles resolver can apply the Member default. Singleton process lifetime matches
// InMemoryPrincipalStore registration (zero-infra default). PostgresDbModule swaps to scoped
// EfPrincipalRoleStore when a connection string is present (task 147-006). Features substrate
// namespace — see IPrincipalRoleStore Design (shared by Identity + Admin without TWA0009).
#endregion

namespace TimeWarp.Architecture.Features;

using System.Collections.Concurrent;
using TimeWarp.Identity;

/// <summary>In-memory principal→role assignment store.</summary>
public sealed class InMemoryPrincipalRoleStore : IPrincipalRoleStore
{
  private readonly ConcurrentDictionary<PrincipalId, Guid[]> Assignments = new();

  public Task<IReadOnlyList<Guid>> GetRoleIdsAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    if (!Assignments.TryGetValue(principalId, out Guid[]? roles))
    {
      return Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
    }

    return Task.FromResult<IReadOnlyList<Guid>>(roles.ToArray());
  }

  public Task SetRoleIdsAsync(
    PrincipalId principalId,
    IReadOnlyList<Guid> roleIds,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(roleIds);
    cancellationToken.ThrowIfCancellationRequested();

    Guid[] snapshot = roleIds.Distinct().ToArray();
    if (snapshot.Length == 0)
    {
      Assignments.TryRemove(principalId, out _);
    }
    else
    {
      Assignments[principalId] = snapshot;
    }

    return Task.CompletedTask;
  }
}
