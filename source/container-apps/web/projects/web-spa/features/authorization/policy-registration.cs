#region Purpose
// Composition root for all client-side authorization policies.
#endregion

#region Design
// Anonymous = always true. All role-gated policies register through RolePolicyGrants so
// page/nav/developer extras cannot drift (task 147-002).
#endregion

namespace TimeWarp.Architecture;

using TimeWarp.Architecture.Features.Authorization;
using static AuthorizationConstants.Policies;

internal static class PolicyRegistration
{
  public static void AddPolicies(AuthorizationOptions options)
  {
    options.AddPolicy(
      Anonymous,
      policy => policy.RequireAssertion(static _ => true));

    // Any signed-in principal (Member claim or mock/Entra). Not role-mapped.
    options.AddPolicy(
      Authenticated,
      policy => policy.RequireAuthenticatedUser());

    // Documented registration sites (no-op grant lists for grep discoverability).
    PagePolicyRegistration.AddPolicies(options);
    NavigationPolicyRegistration.AddPolicies(options);

    // SSOT: every role-gated SPA policy (self-service + admin + developer).
    RolePolicyGrants.AddAllGrantedPolicies(options);
  }
}
