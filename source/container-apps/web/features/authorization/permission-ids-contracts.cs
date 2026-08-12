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
}
