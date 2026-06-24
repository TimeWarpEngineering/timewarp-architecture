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
