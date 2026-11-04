#region Purpose
// Skip-mode IRoleAgentScopeStore: reads a role's agent scopes from the in-memory Role aggregate store.
#endregion

#region Design
// Task 473: same split as InMemoryRolePermissionStore. Agent-scope approval reads this platform
// port; the Roles slice owns the data and writes through IRoleStore (SetRoleAgentScopes). Product
// roles start with no agent scopes, so a new agent whose effective role is Member may request
// only identity:read. Roles with no scopes are omitted from the batch read. Singleton over the
// singleton InMemoryRoleStore; PostgresDbModule replaces it with EfRoleAgentScopeStore.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Authorization;
using TimeWarp.Architecture.Features.Admin.Roles.Domain;

/// <summary>Role agent-scope reads over <see cref="InMemoryRoleStore"/>.</summary>
public sealed class InMemoryRoleAgentScopeStore : IRoleAgentScopeStore
{
  private readonly InMemoryRoleStore Roles;

  public InMemoryRoleAgentScopeStore(InMemoryRoleStore roles)
  {
    Roles = roles ?? throw new ArgumentNullException(nameof(roles));
  }

  public async Task<IReadOnlyList<string>> GetScopesForRoleAsync(
    Guid roleId,
    CancellationToken cancellationToken = default)
  {
    Role? role = await Roles.FindAsync(RoleId.From(roleId), cancellationToken).ConfigureAwait(false);
    return role is null ? [] : [.. role.AgentScopes.Select(scope => scope.Scope)];
  }

  public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetScopesForRolesAsync(
    IReadOnlyCollection<Guid> roleIds,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(roleIds);
    IReadOnlyList<Role> roles = await Roles
      .FindManyAsync([.. roleIds.Distinct().Select(RoleId.From)], cancellationToken)
      .ConfigureAwait(false);

    return roles
      .Where(role => role.AgentScopes.Count > 0)
      .ToDictionary(
        role => role.Id.Value,
        role => (IReadOnlyList<string>)[.. role.AgentScopes.Select(scope => scope.Scope)]);
  }
}
