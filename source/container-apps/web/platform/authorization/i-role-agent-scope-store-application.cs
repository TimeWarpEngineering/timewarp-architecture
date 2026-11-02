#region Purpose
// Port for the agent scopes a product role may contribute to an agent token.
#endregion

#region Design
// Task 466: separate from IRolePermissionStore. Human permission ids never flow onto an agent
// token; this list is the only role field issuance reads. The store is a dumb replace-set.
// In-memory by default; PostgresDbModule swaps in EfRoleAgentScopeStore when connected.
#endregion

namespace TimeWarp.Architecture.Authorization;

/// <summary>Durable assignment of agent scopes to a product role.</summary>
public interface IRoleAgentScopeStore
{
  /// <summary>Stored agent scopes for the role — empty when nothing has been granted.</summary>
  Task<IReadOnlyList<string>> GetScopesForRoleAsync(
    Guid roleId,
    CancellationToken cancellationToken = default);

  /// <summary>Stored agent scopes for each requested role. Roles with none are omitted.</summary>
  Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetScopesForRolesAsync(
    IReadOnlyCollection<Guid> roleIds,
    CancellationToken cancellationToken = default);

  /// <summary>Replace stored agent scopes for the role (empty clears all grants for that role).</summary>
  Task SetScopesForRoleAsync(
    Guid roleId,
    IReadOnlyList<string> scopes,
    CancellationToken cancellationToken = default);
}
