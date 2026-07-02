#region Purpose
// BaseComponent partial: imperative authentication/authorization checks for component logic.
#endregion

#region Design
// Complements declarative AuthorizeView for decisions that live in code rather than markup —
// e.g. whether to dispatch an action or compute a value — without duplicating the check pattern
// in every component.
// A null policy means "authenticated at all", so callers are not forced to invent a policy for
// the common signed-in/anonymous branch.
#endregion

namespace TimeWarp.Architecture.Features;

partial class BaseComponent
{
  [Inject] protected IAuthorizationService AuthorizationService { get; set; } = null!;
  [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

  /// <summary>
  /// Checks if the user is authorized.
  /// If no policy is provided, it checks for basic authentication.
  /// If a policy is provided, it checks for authorization against that policy.
  /// </summary>
  /// <param name="policy">The policy to check, or null to check only authentication.</param>
  /// <returns>True if authorized, false otherwise.</returns>
  protected async Task<bool> CheckAuthorization(string? policy = null)
  {
    AuthenticationState authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
    ClaimsPrincipal user = authenticationState.User;

    if (policy is null)
    {
      return user.Identity?.IsAuthenticated ?? false;
    }

    AuthorizationResult authResult = await AuthorizationService.AuthorizeAsync(user, policy);
    return authResult.Succeeded;
  }
}
