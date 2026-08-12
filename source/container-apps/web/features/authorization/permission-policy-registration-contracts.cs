#region Purpose
// Registration helpers: each PermissionIds entry becomes an ASP.NET policy (server + SPA).
#endregion

#region Design
// Task 182-001/002/003: one registry (PermissionIds) + dual registration methods:
//   AddPermissionPolicies      — server: PermissionRequirement → IPermissionEvaluator
//   AddPermissionClaimPolicies — SPA: RequireClaim(PermissionIds.ClaimType, id) from session-
//                                projected claims (WASM has no grant store / evaluator)
// Policy name is identity with the permission id (1:1). Scheme lists stay on
// [EndpointAuthorize(AuthenticationSchemes)] for FastEndpoints (task 158); SPA has no scheme
// restriction on policies. Contracts layer holds both helpers so web-server and web-spa share
// one call site; Microsoft.AspNetCore.Authorization is the only host package contracts needs.
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
  /// Server: registers a policy for every <see cref="PermissionIds.All"/> entry, each requiring
  /// that permission via <see cref="PermissionRequirement"/> (handler → IPermissionEvaluator).
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

  /// <summary>
  /// SPA: registers a policy for every <see cref="PermissionIds.All"/> entry that succeeds when
  /// the principal has a <see cref="PermissionIds.ClaimType"/> claim equal to that permission id
  /// (claims projected from GetCurrentSession / mock auth — no evaluator in WASM).
  /// </summary>
  public static void AddPermissionClaimPolicies(AuthorizationOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    foreach (string permissionId in PermissionIds.All)
    {
      options.AddPolicy(
        permissionId,
        policy => policy.RequireClaim(PermissionIds.ClaimType, permissionId));
    }
  }
}
