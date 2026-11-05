#region Purpose
// Domain copies of the catalogs the Role aggregate checks against: permission ids, agent scopes, product role ids.
#endregion

#region Design
// web-domain references foundation-domain only, so it cannot see PermissionIds.All (web-contracts),
// AgentScopes.All (timewarp-identity), or RoleIds.All (web-contracts). These lists are copies, and
// role-catalog-agreement-tests.cs in web-infrastructure-tests (which sees all three assemblies)
// fails when a copy differs from its source in members or order. Add a permission, scope, or
// product role to the source first; the test then names the line to copy here.
// ProductRoleIds backs Role.IsProductRole, the product-role delete guard. AgentScopeRank backs the
// catalog order Role.AgentScopes reads back in (task 478).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Domain;

public static class RoleCatalog
{
  /// <summary>Copy of PermissionIds.All, in the same order.</summary>
  public static IReadOnlyList<string> PermissionIds { get; } =
  [
    "admin.access",
    "admin.roles.read",
    "admin.roles.manage",
    "admin.principals.read",
    "admin.principals.manage",
    "admin.profiles.read",
    "admin.feedback.read",
    "admin.feedback.manage",
    "developer.access",
    "developer.claims.read",
    "profile.read",
    "profile.write",
    "settings.read",
    "settings.write",
    "agent-link.manage.self",
    "identity.read",
    "credential.manage.self",
    "demo.invoke",
    "feedback.file.self",
    "feedback.read",
    "feedback.note",
    "feedback.severity",
    "feedback.status",
  ];

  /// <summary>Copy of AgentScopes.All, in the same order.</summary>
  public static IReadOnlyList<string> AgentScopes { get; } =
  [
    "identity:read",
    "demo:invoke",
    "credential:manage",
    "feedback:read",
    "feedback:note",
    "feedback:severity",
    "feedback:status",
  ];

  /// <summary>Copy of RoleIds.All (Member, Operator, Administrator, Developer), in the same order.</summary>
  public static IReadOnlyList<RoleId> ProductRoleIds { get; } =
  [
    RoleId.From(new Guid("A1B2C3D4-E5F6-4789-A012-3456789ABCDE")),
    RoleId.From(new Guid("B2C3D4E5-F6A7-4890-B123-456789ABCDEF")),
    RoleId.From(new Guid("834B9073-D5FF-40B3-938A-968C23FA76CC")),
    RoleId.From(new Guid("80EE3E0C-A8B6-45D6-BA27-7DEE2691AA42")),
  ];

  public static bool IsPermissionId(string permissionId) =>
    PermissionIds.Contains(permissionId, StringComparer.Ordinal);

  public static bool IsAgentScope(string scope) =>
    AgentScopes.Contains(scope, StringComparer.Ordinal);

  /// <summary>Position of <paramref name="scope"/> in <see cref="AgentScopes"/>; int.MaxValue when it is not in the catalog.</summary>
  public static int AgentScopeRank(string scope)
  {
    for (int index = 0; index < AgentScopes.Count; index++)
    {
      if (AgentScopes[index].Equals(scope, StringComparison.Ordinal))
      {
        return index;
      }
    }

    return int.MaxValue;
  }

  public static bool IsProductRole(RoleId id) => ProductRoleIds.Contains(id);
}
