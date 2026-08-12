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
// Full policy registration (PermissionRequirement handler) lands in 182-002; this registry is
// the only SSOT for permission strings starting now.
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>Stable permission (capability) identifiers for authorization policies.</summary>
public static class PermissionIds
{
  public const string AdminAccess = "admin.access";
  public const string AdminRolesRead = "admin.roles.read";
  public const string AdminRolesManage = "admin.roles.manage";
  public const string AdminPrincipalsRead = "admin.principals.read";
  public const string AdminPrincipalsManage = "admin.principals.manage";
  public const string DeveloperAccess = "developer.access";
  public const string DeveloperClaimsRead = "developer.claims.read";
  public const string ProfileRead = "profile.read";
  public const string SettingsRead = "settings.read";

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
  ];
}
