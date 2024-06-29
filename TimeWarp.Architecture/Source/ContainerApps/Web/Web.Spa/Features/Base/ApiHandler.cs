namespace TimeWarp.Architecture.Features;
using FluentValidation.Results;

internal abstract class ApiHandler<TAction, TRequest, TResponse> : BaseHandler<TAction>
  where TAction : IBaseAction
  where TRequest : IApiRequest
  where TResponse : class
{
  private readonly AuthenticationStateProvider? AuthenticationStateProvider;
  private readonly IApiService ApiService;
  private readonly IValidator<TRequest>? Validator;
  private bool RequiresAuthentication => AuthenticationStateProvider is not null;

  protected ApiHandler
  (
    IStore store,
    IApiService apiService,
    IValidator<TRequest>? validator = null,
    AuthenticationStateProvider? authenticationStateProvider = null
  ) : base(store)
  {
    AuthenticationStateProvider = authenticationStateProvider;
    ApiService = apiService;
    Validator = validator;
  }

  public sealed override async Task Handle(TAction action, CancellationToken cancellationToken)
  {
    if (RequiresAuthentication && !await IsUserAuthenticatedAsync()) return;

    // get the semaphore so we only call the API one at a time
    Type currentType = typeof(TAction).GetEnclosingStateType();
    SemaphoreSlim semaphore = Store.GetSemaphore(currentType);
    await semaphore.WaitAsync(cancellationToken);
    try
    {
      TRequest? request = await GetRequest(action, cancellationToken);
      if (request is null) return;// Skip the action

      ValidationResult? x = Validator?.Validate(request);
      if (x?.IsValid == false)
      {
        throw new ValidationException(x.Errors);
      }

      OneOf<TResponse, FileResponse, SharedProblemDetails> apiResponse =
        await ApiService.GetResponse<TResponse>
        (
          request,
          cancellationToken
        );

      apiResponse.Switch
      (
        response => HandleSuccess(response, cancellationToken),
        fileResponse => HandleFileResponse(fileResponse, cancellationToken),
        problemDetails => HandleError(problemDetails, cancellationToken)
      );
    }
    finally
    {
      semaphore.Release();
    }
  }

  private async Task<bool> IsUserAuthenticatedAsync()
  {
    AuthenticationState authState = await AuthenticationStateProvider!.GetAuthenticationStateAsync();
    ClaimsPrincipal user = authState.User;
    return user.Identity?.IsAuthenticated ?? false;
  }

  /// <summary>
  /// Get the request object to send to the API
  /// If null is returned, the action will be skipped
  /// </summary>
  /// <param name="action"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  protected abstract Task<TRequest?> GetRequest(TAction action, CancellationToken cancellationToken);
  protected abstract Task HandleSuccess(TResponse response, CancellationToken cancellationToken);
  protected abstract Task HandleFileResponse(FileResponse fileResponse, CancellationToken cancellationToken);
  protected abstract Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken);
}
