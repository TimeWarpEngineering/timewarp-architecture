#region Purpose
// Skeleton SSOT list of permission policy names (1:1 with PermissionIds) for SPA/server registration.
#endregion

#region Design
// Task 182-001: disposition requires a single registry + single registration helper that eventually
// replaces AuthorizationConstants.Policies, AuthorizationPolicyNames, and RolePolicyGrants.
// This child only exposes AllPermissionPolicyNames (= PermissionIds.All). Full AddPermissionPolicies
// helper and PermissionRequirement handler land in 182-002 (server) / 182-003 (SPA) — wiring them
// here would either no-op or change enforcement, which 182-001 forbids.
// Contracts layer so both hosts and SPA can reference the list without application coupling.
// Features substrate namespace (same family as PermissionIds).
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>
/// Permission-backed ASP.NET policy names (identity with <see cref="PermissionIds"/>).
/// Registration helpers arrive in 182-002 / 182-003.
/// </summary>
public static class PermissionPolicyRegistration
{
  /// <summary>Every permission id is a policy name (1:1 disposition).</summary>
  public static IReadOnlyList<string> AllPermissionPolicyNames => PermissionIds.All;
}
