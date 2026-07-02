#region Purpose
// Resolves the signed-in user's id as a Guid from whichever identifier claim the issuer provides.
#endregion

#region Design
// Declared in the Microsoft.AspNetCore.Components.Authorization namespace so the extension is
// discoverable anywhere AuthenticationStateProvider is already in scope, with no extra using.
// Claim probe order sub -> oid -> NameIdentifier covers standard OIDC, Microsoft Entra, and
// ASP.NET Identity token shapes without caller-side branching.
// Throws rather than returning a nullable: callers need an authenticated identity, and a missing
// id claim is a configuration error, not a normal state.
#endregion

namespace Microsoft.AspNetCore.Components.Authorization;

public static class AuthenticationStateProviderExtensions
{
  public static async Task<Guid> GetUserIdAsync(this AuthenticationStateProvider authenticationStateProvider)
  {
    AuthenticationState authState = await authenticationStateProvider.GetAuthenticationStateAsync();
    ClaimsPrincipal user = authState.User;

    // Try to find a claim that can serve as a unique identifier
    Claim idClaim =
      user.FindFirst("sub") ??
      user.FindFirst("oid") ??
      user.FindFirst(ClaimTypes.NameIdentifier) ??
      throw new InvalidOperationException("User does not have an identifiable claim (oid, sub, or nameidentifier).");

    return Guid.Parse(idClaim.Value);
  }
}
