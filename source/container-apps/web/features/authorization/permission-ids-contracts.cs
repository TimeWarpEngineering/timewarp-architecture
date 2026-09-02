#region Purpose
// Compile-time permission (capability) id registry shared by contracts, SPA, and evaluator.
#endregion

#region Design
// Task 182-001 / disposition: dotted lowercase `<area>.<concern>.<verb>` strings are the
// product vocabulary for enforcement (policy name == permission id). Not Guid — OpenFGA/Cedar
// and human-readable logs want strings; stability is "do not rename issued ids," same policy as
// RoleIds. Features substrate (bare …Features) so Admin/Identity/SPA can reference without
// TWA0009. Admin read/manage split is intentional teaching surface (roles.read ≠ roles.manage).
// All is the ordered catalog for seed UIs, SPA projection, and evaluator output stability.
// ClaimType is the SPA claim type projected from GetCurrentSession.Permissions (182-003) —
// server enforcement never reads permission claims; it always routes through IPermissionEvaluator.
// Policy registration: AddPermissionPolicies (server requirement) + AddPermissionClaimPolicies
// (SPA RequireClaim). This registry is the only SSOT for permission strings.
// Agent-facing ids (182-006): identity.read, credential.manage.self, demo.invoke map from
// AgentScopes via AgentScopePermissionSeed; humans receive credential.manage.self in
// SelfServicePermissions (dual-scheme credential surface).
// GroupsByPrefix / Prefix (task 206): derived from All's first dotted segment — not a second
// catalog. SPA list chips and the role-detail parent checkboxes consume this. Protected-core
// UI lock is Administrator + prefix "admin". That set must equal RolePermissionSeed.AdminPermissions
// (pinned in set-role-permissions-tests); contracts cannot reference the application seed.
// Server still enforces ProtectedCoreConflict.
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>Stable permission (capability) identifiers for authorization policies.</summary>
public static class PermissionIds
{
  /// <summary>
  /// Claim type for SPA-projected permission grants (from session response). Not used by
  /// server <see cref="PermissionRequirementHandler"/> (evaluator only).
  /// </summary>
  public const string ClaimType = "permission";

  public const string AdminAccess = "admin.access";
  public const string AdminRolesRead = "admin.roles.read";
  public const string AdminRolesManage = "admin.roles.manage";
  public const string AdminPrincipalsRead = "admin.principals.read";
  public const string AdminPrincipalsManage = "admin.principals.manage";
  public const string DeveloperAccess = "developer.access";
  public const string DeveloperClaimsRead = "developer.claims.read";
  public const string ProfileRead = "profile.read";
  public const string SettingsRead = "settings.read";
  /// <summary>Agent/human self-lookup of principal identity (maps from agent scope identity:read).</summary>
  public const string IdentityRead = "identity.read";
  /// <summary>Manage own credentials (list/add/revoke); maps from agent scope credential:manage.</summary>
  public const string CredentialManageSelf = "credential.manage.self";
  /// <summary>Invoke metered demo capability (maps from agent scope demo:invoke).</summary>
  public const string DemoInvoke = "demo.invoke";

  /// <summary>All product permission ids (stable catalog order).</summary>
  public static IReadOnlyList<string> All { get; } =
  [
    AdminAccess,
    AdminRolesRead,
    AdminRolesManage,
    AdminPrincipalsRead,
    AdminPrincipalsManage,
    DeveloperAccess,
    DeveloperClaimsRead,
    ProfileRead,
    SettingsRead,
    IdentityRead,
    CredentialManageSelf,
    DemoInvoke,
  ];

  /// <summary>
  /// Catalog grouped by first dotted segment (<c>admin</c>, <c>developer</c>, …), preserving
  /// <see cref="All"/> order. Derived — not a second permission SSOT.
  /// </summary>
  public static IReadOnlyList<PermissionGroup> GroupsByPrefix { get; } = BuildGroupsByPrefix();

  /// <summary>First dotted segment of a permission id (<c>admin.roles.manage</c> → <c>admin</c>).</summary>
  public static string Prefix(string permissionId)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);
    int dot = permissionId.IndexOf('.', StringComparison.Ordinal);
    return dot < 0 ? permissionId : permissionId[..dot];
  }

  /// <summary>
  /// Distinct prefixes present in <paramref name="permissionIds"/>, ordered.
  /// Empty when the set is empty (list-row chips).
  /// </summary>
  public static IReadOnlyList<string> PrefixesOf(IEnumerable<string> permissionIds)
  {
    ArgumentNullException.ThrowIfNull(permissionIds);
    return
    [
      .. permissionIds
        .Select(Prefix)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(static prefix => prefix, StringComparer.Ordinal)
    ];
  }

  /// <summary>
  /// Administrator core admin.* grants cannot be stripped (task 182-004). Missing cores stay
  /// addable so a damaged bundle can be repaired from the SPA.
  /// </summary>
  public static bool IsProtectedCore(Guid roleId, string permissionId)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);
    return roleId == RoleIds.Administrator
      && Prefix(permissionId).Equals("admin", StringComparison.Ordinal);
  }

  /// <summary>
  /// True when the SPA must disable unchecking this grant: selected protected-core on
  /// Administrator. Missing cores are not locked so they can be restored.
  /// </summary>
  public static bool IsProtectedCoreLocked(Guid roleId, string permissionId, bool isSelected) =>
    isSelected && IsProtectedCore(roleId, permissionId);

  /// <summary>One prefix group from <see cref="All"/> (parent checkbox + atoms).</summary>
  public readonly record struct PermissionGroup(string Prefix, IReadOnlyList<string> PermissionIds)
  {
    /// <summary>
    /// Parent checkbox state: <c>true</c> all selected, <c>false</c> none, <c>null</c> mixed.
    /// </summary>
    public bool? CheckStateFor(IReadOnlyCollection<string> selected)
    {
      ArgumentNullException.ThrowIfNull(selected);
      int count = 0;
      foreach (string permissionId in PermissionIds)
      {
        if (selected.Contains(permissionId))
        {
          count++;
        }
      }

      if (count == 0)
      {
        return false;
      }

      if (count == PermissionIds.Count)
      {
        return true;
      }

      return null;
    }
  }

  private static List<PermissionGroup> BuildGroupsByPrefix()
  {
    List<PermissionGroup> groups = [];
    foreach (IGrouping<string, string> grouping in All.GroupBy(Prefix, StringComparer.Ordinal))
    {
      groups.Add(new PermissionGroup(grouping.Key, [.. grouping]));
    }

    return groups;
  }
}
