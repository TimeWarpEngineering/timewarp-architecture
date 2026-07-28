#region Purpose
// Composition root for all client-side authorization policies.
#endregion

#region Design
// An explicit always-true Anonymous policy lets every guarded surface declare a policy name
// uniformly instead of special-casing "no policy required".
// Delegates to per-concern registration classes (navigation, pages) so each list stays small
// and a feature's policies are found by concern, not by scanning one large method.
#endregion

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
