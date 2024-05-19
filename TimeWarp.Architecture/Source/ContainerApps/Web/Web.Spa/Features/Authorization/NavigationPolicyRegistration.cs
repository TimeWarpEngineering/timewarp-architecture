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
