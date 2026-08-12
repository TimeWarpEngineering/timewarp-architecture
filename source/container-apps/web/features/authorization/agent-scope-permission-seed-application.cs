#region Purpose
// Agent scope → permission bundle map (parallel to RolePermissionSeed for human roles).
#endregion

#region Design
// Task 182-006: agent-token grants come ONLY from scopes on the presented bearer token, expanded
// via this seed — never from human role membership or IRolePermissionStore. Keys are
// TimeWarp.Identity.AgentScopes string constants; values are PermissionIds bundles (no admin.*).
// Expand unions known scopes, ignores unknown, orders by PermissionIds.All for stable session/
// test output. Invariant (pinned by tests): map values ∩ RolePermissionSeed.AdminPermissions is
// empty so no agent token can hold admin.* via scope seed alone.
// Features substrate — same consumers as PermissionIds / PermissionEvaluator without TWA0009.
#endregion

namespace TimeWarp.Architecture.Features;

using TimeWarp.Identity;

/// <summary>Compile-time default grants: agent scope string → permission id strings.</summary>
public static class AgentScopePermissionSeed
{
  /// <summary>
  /// Scope → permission ids. Keys cover every <see cref="AgentScopes.All"/> entry so issuance
  /// and evaluation never invent missing product scopes.
  /// </summary>
  public static IReadOnlyDictionary<string, IReadOnlyList<string>> DefaultGrants { get; } =
    new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
    {
      [AgentScopes.IdentityRead] =
      [
        PermissionIds.IdentityRead,
      ],
      [AgentScopes.CredentialManage] =
      [
        PermissionIds.CredentialManageSelf,
      ],
      [AgentScopes.DemoInvoke] =
      [
        PermissionIds.DemoInvoke,
      ],
    };

  /// <summary>
  /// Union permission ids for the given scopes (unknown scopes ignored). Ordered by
  /// <see cref="PermissionIds.All"/> then any non-catalog leftovers.
  /// </summary>
  public static IReadOnlyList<string> Expand(IEnumerable<string> scopes)
  {
    ArgumentNullException.ThrowIfNull(scopes);

    HashSet<string> granted = new(StringComparer.Ordinal);
    foreach (string scope in scopes)
    {
      if (string.IsNullOrWhiteSpace(scope))
      {
        continue;
      }

      if (DefaultGrants.TryGetValue(scope, out IReadOnlyList<string>? permissions))
      {
        foreach (string permissionId in permissions)
        {
          granted.Add(permissionId);
        }
      }
    }

    if (granted.Count == 0)
    {
      return Array.Empty<string>();
    }

    List<string> ordered = [];
    foreach (string catalogId in PermissionIds.All)
    {
      if (granted.Remove(catalogId))
      {
        ordered.Add(catalogId);
      }
    }

    if (granted.Count > 0)
    {
      ordered.AddRange(granted.OrderBy(static id => id, StringComparer.Ordinal));
    }

    return ordered;
  }
}
