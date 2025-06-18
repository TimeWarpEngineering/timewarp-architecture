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
