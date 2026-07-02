#region Purpose
// Maps page-access policies to the roles that satisfy them.
#endregion

#region Design
// One policy per routable page keeps the role-to-page mapping in a single registration site;
// pages declare a policy name and never a role, so role restructuring touches only this file.
// Registered separately from navigation policies because a page must stay guarded even when
// its nav entry is hidden — direct URL access bypasses the sidebar.
#endregion

namespace TimeWarp.Architecture;

using static Policies;
using static RoleIds;

internal static class PagePolicyRegistration
{
  public static void AddPolicies(AuthorizationOptions options)
  {
    options.AddPolicy(CanViewAdminPage, policy => policy.RequireRole(Administrator.ToString()));
    options.AddPolicy(CanViewDeveloperPage, policy => policy.RequireRole(Developer.ToString()));
    options.AddPolicy(CanViewUserClaimsPage, policy => policy.RequireRole(Developer.ToString()));
    options.AddPolicy(CanViewRolesPage, policy => policy.RequireRole(Administrator.ToString()));
  }
}
