#region Purpose
// Maps sidebar-navigation visibility policies to the roles that satisfy them.
#endregion

#region Design
// Policies are named per UI surface (a nav section) rather than per role, so markup checks a
// stable policy name while the role composition can change here in one place.
// Kept separate from page policies: hiding a nav entry is a distinct decision from blocking
// the page route it links to, and the two may diverge.
#endregion

namespace TimeWarp.Architecture;

using static Policies;
using static RoleIds;

internal static class NavigationPolicyRegistration
{
  internal static void AddPolicies(AuthorizationOptions options)
  {
    options.AddPolicy(CanViewDeveloperSidebarNavSection, policy => policy.RequireRole(Developer.ToString()));
    options.AddPolicy(CanViewAdminSidebarNavSection, policy => policy.RequireRole(Administrator.ToString()));
  }
}
