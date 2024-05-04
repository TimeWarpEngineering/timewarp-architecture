namespace TimeWarp.Architecture.Features;

internal abstract class FetchHandler<TAction, TResponse, TRequest> : BaseHandler<TAction>
    where TAction : IBaseAction
    where TResponse : class
    where TRequest : IApiRequest
{
    private readonly AuthenticationStateProvider AuthenticationStateProvider;
    private readonly IApiService ApiService;
    private readonly bool RequiresAuthentication;

    protected FetchHandler
    (
      IStore store,
      AuthenticationStateProvider authenticationStateProvider,
      IApiService apiService,
      ISender sender,
      bool requiresAuthentication = false
    ) : base(store)
    {
        AuthenticationStateProvider = authenticationStateProvider;
        ApiService = apiService;
        RequiresAuthentication = requiresAuthentication;
    }

    public sealed override async Task Handle(TAction action, CancellationToken cancellationToken)
    {
        if (RequiresAuthentication && !await IsUserAuthenticatedAsync()) return;

        TRequest? request = await GetRequest(action, cancellationToken);
        if (request is null) return; // Skip the action

        OneOf<TResponse, SharedProblemDetails> apiResponse =
          await ApiService.GetResponse<TResponse>
          (
            request,
            cancellationToken
          );

        apiResponse.Switch
        (
          response => HandleSuccess(response, cancellationToken),
          problemDetails => HandleError(problemDetails, cancellationToken)
        );
    }

    private async Task<bool> IsUserAuthenticatedAsync()
    {
        AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        ClaimsPrincipal user = authState.User;
        return user.Identity?.IsAuthenticated ?? false;
    }

    protected abstract Task<TRequest?> GetRequest(TAction action, CancellationToken cancellationToken);
    protected abstract Task HandleSuccess(TResponse success, CancellationToken cancellationToken);
    protected abstract Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken);
}
