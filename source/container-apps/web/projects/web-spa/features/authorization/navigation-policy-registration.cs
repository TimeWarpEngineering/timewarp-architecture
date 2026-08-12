#region Purpose
// Documents sidebar navigation policies; grants live in RolePolicyGrants.
#endregion

#region Design
// Nav section visibility is separate from page route policy names, but both draw roles from
// RolePolicyGrants (task 147-002). Hiding a nav entry does not replace page [Authorize].
#endregion

namespace TimeWarp.Architecture;

using static AuthorizationConstants.Policies;

internal static class NavigationPolicyRegistration
{
  internal static void AddPolicies(AuthorizationOptions options)
  {
    _ = options; // Avoids "unused parameter" warning; the method is a placeholder for documentation and future use.
    _ = new[]
    {
      CanViewDeveloperSidebarNavSection,
      CanViewAdminSidebarNavSection,
    };
    // Actual registration is centralized in PolicyRegistration → RolePolicyGrants.
  }
}
