namespace TimeWarp.Architecture.Features;

partial class BaseComponent
{
  [Inject] protected IAuthorizationService AuthorizationService { get; set; } = null!;
  [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

  protected async Task<bool> CheckAuthorization(string policy)
  {
    AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
    ClaimsPrincipal user = authState.User;

    AuthorizationResult authResult = await AuthorizationService.AuthorizeAsync(user, policy);
    return authResult.Succeeded;
  }
}
