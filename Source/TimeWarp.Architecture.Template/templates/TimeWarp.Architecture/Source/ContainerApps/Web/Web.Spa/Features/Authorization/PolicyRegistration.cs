namespace TimeWarp.Architecture;

internal static class PolicyRegistration
{
  public static void AddPolicies(AuthorizationOptions options)
  {
    NavigationPolicyRegistration.AddPolicies(options);
    PagePolicyRegistration.AddPolicies(options);

    // Developer
    options.AddPolicy
    (
      Policies.CanViewUserClaims,
      policy => policy.RequireRole(RoleIds.Developer.ToString())
    );
  }
}
