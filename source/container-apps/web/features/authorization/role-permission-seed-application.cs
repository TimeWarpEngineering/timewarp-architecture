#region Purpose
// Default role → permission grant map for greenfield bootstrap and in-memory store seed.
#endregion

#region Design
// Task 182-001: product roles (RoleIds) are mutable bundles of PermissionIds. This map is the
// template seed that keeps server + SPA permission expansion observably equivalent to the
// pre-182 role-gated surface (Administrator → admin.*, Developer → developer.*, all → self-service).
//   Administrator — all admin.* + self-service (first Create claims this role)
//   Member        — self-service only (default effective role when store empty)
//   Developer     — developer.* + self-service (demos / diagnostics)
//   Operator      — self-service only until marketplace policies (118); Operator-only grants reserved
// InMemoryRolePermissionStore copies this on construction; EF migration InsertData mirrors it
// for postgres volumes. Admin UI (182-004) will mutate grants per role after lockout guards land.
// Features substrate — same consumers as RoleIds / IRolePermissionStore without TWA0009.
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>Compile-time default grants: product role Guid → permission id strings.</summary>
public static class RolePermissionSeed
{
  /// <summary>Self-service permissions every product human role receives by default.</summary>
  public static IReadOnlyList<string> SelfServicePermissions { get; } =
  [
    PermissionIds.ProfileRead,
    PermissionIds.SettingsRead,
  ];

  /// <summary>All admin.* permissions (Administrator seed; protected-core target in 182-004).</summary>
  public static IReadOnlyList<string> AdminPermissions { get; } =
  [
    PermissionIds.AdminAccess,
    PermissionIds.AdminRolesRead,
    PermissionIds.AdminRolesManage,
    PermissionIds.AdminPrincipalsRead,
    PermissionIds.AdminPrincipalsManage,
  ];

  /// <summary>Developer demo/diagnostics permissions.</summary>
  public static IReadOnlyList<string> DeveloperPermissions { get; } =
  [
    PermissionIds.DeveloperAccess,
    PermissionIds.DeveloperClaimsRead,
  ];

  /// <summary>
  /// RoleId → permission ids. Keys cover every <see cref="RoleIds.All"/> entry so seed UIs
  /// and stores never invent missing product roles.
  /// </summary>
  public static IReadOnlyDictionary<Guid, IReadOnlyList<string>> DefaultGrants { get; } =
    new Dictionary<Guid, IReadOnlyList<string>>
    {
      [RoleIds.Administrator] =
      [
        .. AdminPermissions,
        .. SelfServicePermissions,
      ],
      [RoleIds.Member] =
      [
        .. SelfServicePermissions,
      ],
      [RoleIds.Developer] =
      [
        .. DeveloperPermissions,
        .. SelfServicePermissions,
      ],
      [RoleIds.Operator] =
      [
        // Marketplace ops grants reserved until 118; self-service only for now.
        .. SelfServicePermissions,
      ],
    };
}
