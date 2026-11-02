#region Purpose
// Port for agent scopes granted directly on a principal, on top of its roles.
#endregion

#region Design
// Task 466: the admin Principal page replaces this list. Issuance unions it with identity:read
// and the principal's role agent scopes. The store is a dumb replace-set. In-memory by default;
// PostgresDbModule swaps in EfPrincipalAgentScopeStore when connected.
#endregion

namespace TimeWarp.Architecture.Authorization;

using TimeWarp.Identity;

/// <summary>Durable assignment of agent scopes to one principal.</summary>
public interface IPrincipalAgentScopeStore
{
  /// <summary>Stored agent scopes for the principal — empty when nothing has been granted.</summary>
  Task<IReadOnlyList<string>> GetScopesAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default);

  /// <summary>Replace stored agent scopes for the principal (empty clears them).</summary>
  Task SetScopesAsync(
    PrincipalId principalId,
    IReadOnlyList<string> scopes,
    CancellationToken cancellationToken = default);
}
