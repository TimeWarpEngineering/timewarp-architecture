#region Purpose
// Composition root for all client-side authorization policies.
#endregion

#region Design
// Anonymous = always true; Authenticated = any signed-in principal (not permission-mapped).
// Permission-backed page/nav policies (task 182-003): policy name == PermissionIds.
// Pure WASM SPA: AddPermissionClaimPolicies (RequireClaim from session-projected claims —
// no evaluator in the browser). Web.Server composes this same ConfigureServices for
// prerender AFTER AddPermissionPolicies — overwriting would replace PermissionRequirement
// with RequireClaim and break server admin APIs (evaluator path). Skip claim policies when
// a permission id is already registered (server PermissionRequirement wins; prerender
// AuthorizeView then uses the evaluator against the cookie principal, which is correct).
#endregion

namespace TimeWarp.Architecture;

using TimeWarp.Architecture.Features;
using static AuthorizationConstants.Policies;

internal static class PolicyRegistration
{
  public static void AddPolicies(AuthorizationOptions options)
  {
    options.AddPolicy(
      Anonymous,
      policy => policy.RequireAssertion(static _ => true));

    // Any signed-in principal (identity-session, mock, or Entra). Not permission-mapped.
    options.AddPolicy(
      Authenticated,
      policy => policy.RequireAuthenticatedUser());

    // Pure WASM: register claim policies. Hosted under web-server (prerender): keep server's
    // PermissionRequirement policies — do not overwrite with RequireClaim.
    if (options.GetPolicy(PermissionIds.AdminAccess) is null)
    {
      PermissionPolicyRegistration.AddPermissionClaimPolicies(options);
    }
  }
}
