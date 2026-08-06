#region Purpose
// Registers page-access policies from RolePolicyGrants (role composition SSOT).
#endregion

#region Design
// One policy per routable surface; pages declare policy names only. Role→policy pairing is
// RolePolicyGrants (task 147-002) so restructuring roles never reopens this file for RequireRole.
// Page route guard and nav Policy on [Page] must use the same policy name.
#endregion

namespace TimeWarp.Architecture;

using static AuthorizationConstants.Policies;

internal static class PagePolicyRegistration
{
  public static void AddPolicies(AuthorizationOptions options)
  {
    // Registered via RolePolicyGrants.AddAllGrantedPolicies in PolicyRegistration —
    // page-specific names documented here for discoverability.
    _ = new[]
    {
      CanViewOwnProfile,
      CanViewSettings,
      CanViewAdminPage,
      CanViewDeveloperPage,
      CanViewUserClaimsPage,
      CanViewRolesPage,
      CanViewPrincipalsPage,
    };
    // Actual registration is centralized in PolicyRegistration → RolePolicyGrants.
  }
}
