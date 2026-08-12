#region Purpose
// Single source of truth: which product roles satisfy each SPA authorization policy.
#endregion

#region Design
// Task 147-002: policies stay capability-named (CanViewDeveloperPage); roles are assigned to
// principals; this map is the only place that pairs them. Page/nav registration must call
// RolePolicyGrants.AddPolicy so role restructuring never edits scattered RequireRole lines.
// Multiple roles per policy = OR (ASP.NET Core RequireRole params).
// Self-service policies grant all product roles (Member + Operator + Admin + Developer) so any
// signed-in human can open Profile/Settings; admin/dev nav stay role-narrow.
// Operator-only marketplace policies land with 118.
// Policies.Authenticated is NOT here — it is RequireAuthenticatedUser in PolicyRegistration.
#endregion

namespace TimeWarp.Architecture.Features.Authorization;

using Microsoft.AspNetCore.Authorization;
using static TimeWarp.Architecture.AuthorizationConstants.Policies;
using static TimeWarp.Architecture.Features.RoleIds;

/// <summary>Maps policy name → role Guids that satisfy it.</summary>
public static class RolePolicyGrants
{
  /// <summary>Every product human role (default passkey Member through Admin/Developer).</summary>
  public static IReadOnlyList<Guid> AllProductRoles { get; } =
  [
    Member,
    Operator,
    Administrator,
    Developer,
  ];

  /// <summary>
  /// Policy → roles that grant it. Add new SPA policies here and in
  /// <see cref="AuthorizationConstants.Policies"/>; never RequireRole inline elsewhere.
  /// </summary>
  public static IReadOnlyDictionary<string, IReadOnlyList<Guid>> Grants { get; } =
    new Dictionary<string, IReadOnlyList<Guid>>(StringComparer.Ordinal)
    {
      // Self-service (147-003)
      [CanViewOwnProfile] = AllProductRoles,
      [CanViewSettings] = AllProductRoles,

      // Admin
      [CanViewAdminSidebarNavSection] = [Administrator],
      [CanViewAdminPage] = [Administrator],
      [CanViewRolesPage] = [Administrator],
      [CanViewPrincipalsPage] = [Administrator],

      // Developer / demos (147-001)
      [CanViewDeveloperSidebarNavSection] = [Developer],
      [CanViewDeveloperPage] = [Developer],
      [CanViewUserClaimsPage] = [Developer],
      [CanViewUserClaims] = [Developer],
    };

  /// <summary>Registers a policy that succeeds when the user has any of the granted roles.</summary>
  public static void AddPolicy(AuthorizationOptions options, string policyName)
  {
    if (!Grants.TryGetValue(policyName, out IReadOnlyList<Guid>? roles) || roles.Count == 0)
    {
      throw new InvalidOperationException(
        $"RolePolicyGrants has no roles for policy '{policyName}'. Add a grant map entry.");
    }

    string[] roleStrings = roles.Select(static r => r.ToString()).ToArray();
    options.AddPolicy(policyName, policy => policy.RequireRole(roleStrings));
  }

  /// <summary>Registers every grant-mapped policy (page + nav + developer extras).</summary>
  public static void AddAllGrantedPolicies(AuthorizationOptions options)
  {
    foreach (string policyName in Grants.Keys)
    {
      AddPolicy(options, policyName);
    }
  }
}
