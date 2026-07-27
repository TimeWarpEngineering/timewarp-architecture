#region Purpose
// Handler base that silently drops actions when the user is not authenticated.
#endregion

#region Design
// No error is surfaced by design: components rendered before sign-in completes can fire actions,
// and treating those as failures would produce noise for an expected condition.
// Intended for handlers that gate on identity without calling the API; ApiHandler embeds the
// same optional gate for handlers that do.
#endregion

namespace TimeWarp.Architecture.Features;

internal abstract class AuthenticatedHandler<TAction> : BaseHandler<TAction>
  where TAction : IBaseAction
{
  private readonly AuthenticationStateProvider AuthenticationStateProvider;

  protected AuthenticatedHandler(IStore store, AuthenticationStateProvider authenticationStateProvider) : base(store)
  {
    AuthenticationStateProvider = authenticationStateProvider;
  }

  public sealed override async Task Handle(TAction action, CancellationToken cancellationToken)
  {
    if (await IsUserAuthenticatedAsync())
    {
      await HandleAuthenticated(action, cancellationToken);
    }
  }

  protected abstract Task HandleAuthenticated(TAction action, CancellationToken cancellationToken);

  private async Task<bool> IsUserAuthenticatedAsync()
  {
    AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
    ClaimsPrincipal user = authState.User;
    return user.Identity?.IsAuthenticated ?? false;
  }
}
