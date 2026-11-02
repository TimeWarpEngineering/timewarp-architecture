#region Purpose
// Unions identity:read, effective-role agent scopes, and principal agent-scope grants.
#endregion

#region Design
// Task 466: role scopes come from IEffectiveRolesResolver, not the raw stored list, so a
// roleless human is Member and a roleless agent has no role. A custom role id the resolver drops does not
// approve anything. Principal grants are ordered with AgentScopeOrder. Effective scopes follow
// AgentScopes.All. Scoped because the EF stores are scoped; the in-memory stores are singletons
// and can still be consumed from a scope.
#endregion

namespace TimeWarp.Architecture.Authorization;

using TimeWarp.Architecture.Features;
using TimeWarp.Identity;

/// <summary>Approved agent scopes for one principal.</summary>
public sealed class AgentScopeApproval : IAgentScopeApproval
{
  private readonly IEffectiveRolesResolver EffectiveRoles;
  private readonly IRoleAgentScopeStore RoleScopes;
  private readonly IPrincipalAgentScopeStore PrincipalScopes;

  public AgentScopeApproval(
    IEffectiveRolesResolver effectiveRoles,
    IRoleAgentScopeStore roleScopes,
    IPrincipalAgentScopeStore principalScopes)
  {
    EffectiveRoles = effectiveRoles ?? throw new ArgumentNullException(nameof(effectiveRoles));
    RoleScopes = roleScopes ?? throw new ArgumentNullException(nameof(roleScopes));
    PrincipalScopes = principalScopes ?? throw new ArgumentNullException(nameof(principalScopes));
  }

  public async Task<AgentScopeApprovalSnapshot> GetSnapshotAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default)
  {
    IReadOnlyList<Guid> roleIds = await EffectiveRoles
      .GetEffectiveRoleIdsAsync(principalId, cancellationToken)
      .ConfigureAwait(false);
    IReadOnlyDictionary<Guid, IReadOnlyList<string>> byRole = await RoleScopes
      .GetScopesForRolesAsync(roleIds, cancellationToken)
      .ConfigureAwait(false);
    IReadOnlyList<string> storedGrants = await PrincipalScopes
      .GetScopesAsync(principalId, cancellationToken)
      .ConfigureAwait(false);

    List<string> principalGrants = AgentScopeOrder.OrderKnown(storedGrants);
    List<ApprovedAgentScope> effective = [];
    foreach (string scope in AgentScopes.All)
    {
      List<AgentScopeGrantSource> sources = [];
      if (string.Equals(scope, AgentScopes.IdentityRead, StringComparison.Ordinal))
      {
        sources.Add(new AgentScopeGrantSource(AgentScopeGrantKind.Default, null));
      }

      foreach (Guid roleId in roleIds)
      {
        if (byRole.TryGetValue(roleId, out IReadOnlyList<string>? granted)
          && granted.Contains(scope, StringComparer.Ordinal))
        {
          sources.Add(new AgentScopeGrantSource(AgentScopeGrantKind.Role, roleId));
        }
      }

      if (principalGrants.Contains(scope, StringComparer.Ordinal))
      {
        sources.Add(new AgentScopeGrantSource(AgentScopeGrantKind.Principal, null));
      }

      if (sources.Count > 0)
      {
        effective.Add(new ApprovedAgentScope(scope, sources));
      }
    }

    return new AgentScopeApprovalSnapshot(principalGrants, effective);
  }
}
