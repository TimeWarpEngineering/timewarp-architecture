namespace TimeWarp.Architecture;

internal static class PolicyRegistration
{
  public static void AddPolicies(AuthorizationOptions options)
  {
    // Add Anonymous policy that allows all requests
    options.AddPolicy
    (
      Policies.Anonymous,
      policy => policy.RequireAssertion(context => true)
    );

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
