namespace TimeWarp.Architecture.Features;

internal abstract class FetchHandler<TAction, TRequest, TResponse> : BaseHandler<TAction>
    where TAction : IBaseAction
    where TRequest : IApiRequest
    where TResponse : class
{
    private readonly AuthenticationStateProvider? AuthenticationStateProvider;
    private readonly IApiService ApiService;
    private bool RequiresAuthentication => AuthenticationStateProvider is not null;

    protected FetchHandler
    (
      IStore store,
      IApiService apiService,
      AuthenticationStateProvider? authenticationStateProvider = null
    ) : base(store)
    {
        AuthenticationStateProvider = authenticationStateProvider;
        ApiService = apiService;
    }

    public sealed override async Task Handle(TAction action, CancellationToken cancellationToken)
    {
        if (RequiresAuthentication && !await IsUserAuthenticatedAsync()) return;

        TRequest? request = await GetRequest(action, cancellationToken);
        if (request is null) return; // Skip the action
        if (request is IAuthApiRequest authRequest) authRequest.UserId = await AuthenticationStateProvider!.GetUserIdAsync();

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
        AuthenticationState authState = await AuthenticationStateProvider!.GetAuthenticationStateAsync();
        ClaimsPrincipal user = authState.User;
        return user.Identity?.IsAuthenticated ?? false;
    }

    protected abstract Task<TRequest?> GetRequest(TAction action, CancellationToken cancellationToken);
    protected abstract Task HandleSuccess(TResponse response, CancellationToken cancellationToken);
    protected abstract Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken);
}
