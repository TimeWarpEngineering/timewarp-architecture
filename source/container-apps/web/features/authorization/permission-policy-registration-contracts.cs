#region Purpose
// Single registration helper: each PermissionIds entry becomes an ASP.NET policy.
#endregion

#region Design
// Task 182-001/002: disposition requires one registry + one registration helper that eventually
// replaces AuthorizationConstants.Policies, AuthorizationPolicyNames, and RolePolicyGrants.
// Policy name is identity with the permission id (1:1). Each policy carries only
// PermissionRequirement — scheme lists stay on [EndpointAuthorize(AuthenticationSchemes)] for
// FastEndpoints (task 158); SPA has no scheme restriction on policies.
// Contracts layer holds the helper so web-server and web-spa (182-003) share one call site;
// Microsoft.AspNetCore.Authorization is the only host package contracts needs for this.
// Features substrate namespace (same family as PermissionIds).
#endregion

namespace TimeWarp.Architecture.Features;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Permission-backed ASP.NET policy registration (policy name == <see cref="PermissionIds"/>).
/// </summary>
public static class PermissionPolicyRegistration
{
  /// <summary>Every permission id is a policy name (1:1 disposition).</summary>
  public static IReadOnlyList<string> AllPermissionPolicyNames => PermissionIds.All;

  /// <summary>
  /// Registers a policy for every <see cref="PermissionIds.All"/> entry, each requiring that
  /// permission via <see cref="PermissionRequirement"/>.
  /// </summary>
  public static void AddPermissionPolicies(AuthorizationOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    foreach (string permissionId in PermissionIds.All)
    {
      options.AddPolicy(
        permissionId,
        policy => policy.AddRequirements(new PermissionRequirement(permissionId)));
    }
  }
}
