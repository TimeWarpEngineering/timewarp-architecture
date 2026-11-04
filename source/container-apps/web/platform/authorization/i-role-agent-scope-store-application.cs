#region Purpose
// Read port for the agent scopes a product role may contribute to an agent token.
#endregion

#region Design
// Task 466: separate from IRolePermissionStore. Human permission ids never flow onto an agent
// token; this list is the only role field issuance reads. Task 473 made it read-only: the Role
// aggregate owns the scopes and is written through IRoleStore (SetRoleAgentScopes). The Roles
// slice implements this port (InMemoryRoleAgentScopeStore in skip-mode, EfRoleAgentScopeStore
// over identity.role_agent_scopes when PostgresDbModule is connected), so agent-scope approval
// reads it without depending on that slice.
#endregion

namespace TimeWarp.Architecture.Authorization;

/// <summary>Reads the agent scopes granted to a product role.</summary>
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
}
