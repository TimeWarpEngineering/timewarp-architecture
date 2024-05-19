namespace Microsoft.AspNetCore.Components.Authorization;

[UsedImplicitly]
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
