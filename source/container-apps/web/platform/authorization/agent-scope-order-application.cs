#region Purpose
// Orders a caller-supplied agent-scope list into the canonical AgentScopes.All sequence.
#endregion

#region Design
// Task 466: SetPrincipalAgentScopes and AgentScopeApproval share this filter; their grants do not
// go through the Role aggregate. Unknown strings drop out. Known strings follow AgentScopes.All,
// so a store that returns an arbitrary order still reads back in catalog order. Role scopes are
// ordered by Role.AgentScopes itself (task 478), so the Roles handlers do not call this.
#endregion

namespace TimeWarp.Architecture.Authorization;

using TimeWarp.Identity;

/// <summary>Catalog order for known agent scopes.</summary>
public static class AgentScopeOrder
{
  /// <summary>
  /// Returns the members of <paramref name="scopes"/> that <see cref="AgentScopes.IsKnown"/>
  /// accepts, in <see cref="AgentScopes.All"/> order.
  /// </summary>
  public static List<string> OrderKnown(IReadOnlyList<string> scopes)
  {
    ArgumentNullException.ThrowIfNull(scopes);
    List<string> ordered = [];
    foreach (string scope in AgentScopes.All)
    {
      if (scopes.Contains(scope, StringComparer.Ordinal))
      {
        ordered.Add(scope);
      }
    }

    return ordered;
  }
}
