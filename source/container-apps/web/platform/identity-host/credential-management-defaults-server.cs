#region Purpose
// Historical name for the credential-management surface policy; SSOT is now PermissionIds.
#endregion

#region Design
// Task 104-005 introduced a dual-scheme RequireAssertion policy ("credential-management") that
// admitted identity-session cookie principals always and agent-token principals only with
// AgentScopes.CredentialManage. Task 182-006 retires that special registration: policy name is
// PermissionIds.CredentialManageSelf ("credential.manage.self"), registered via
// AddPermissionPolicies; AuthenticationSchemes (identity-session + agent-token) live on the four
// credential contracts' [EndpointAuthorize]; humans receive the grant from
// RolePermissionSeed.SelfServicePermissions; agents expand scope credential:manage via
// AgentScopePermissionSeed. This type remains only as a documentation anchor / obsolete alias so
// historical comments and tests that named CredentialManagementDefaults still resolve.
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>
/// Obsolete policy alias — prefer <c>PermissionIds.CredentialManageSelf</c> on contracts.
/// </summary>
public static class CredentialManagementDefaults
{
  /// <summary>
  /// Historical alias equal to <c>PermissionIds.CredentialManageSelf</c>. Prefer the permission id.
  /// </summary>
  public const string Policy = "credential.manage.self";
}
