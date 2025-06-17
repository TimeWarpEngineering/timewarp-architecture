namespace TimeWarp.Architecture.Features;

internal abstract class ApiHandler<TAction, TRequest, TResponse> : BaseHandler<TAction>
  where TAction : IBaseAction
  where TRequest : IApiRequest
  where TResponse : class
{
  private readonly AuthenticationStateProvider? AuthenticationStateProvider;
  private readonly IApiService ApiService;
  private readonly ILogger<ApiHandler<TAction, TRequest, TResponse>> Logger;
  private readonly IValidator<TRequest>? Validator;
  private bool RequiresAuthentication => AuthenticationStateProvider is not null;

  protected ApiHandler
  (
    IStore store,
    IApiService apiService,
    ILogger<ApiHandler<TAction, TRequest, TResponse>> logger,
    IValidator<TRequest>? validator = null,
    AuthenticationStateProvider? authenticationStateProvider = null
  ) : base(store)
  {
    AuthenticationStateProvider = authenticationStateProvider;
    ApiService = apiService;
    Logger = logger;
    Validator = validator;
  }

  public sealed override async Task Handle(TAction action, CancellationToken cancellationToken)
  {
    if (RequiresAuthentication && !await IsUserAuthenticatedAsync()) return;

    Type currentType = typeof(TAction).GetEnclosingStateType();

    using SemaphoreGuard semaphoreGuard = await AcquireSemaphoreAsync(currentType, cancellationToken);
    if (!semaphoreGuard.Acquired) return;

    try
    {
      TRequest? request = await GetRequest(action, cancellationToken);
      if (request is null) return;// Skip the action

      await ValidateRequestAsync(request);

      OneOf<TResponse, FileResponse, SharedProblemDetails> apiResponse =
        await ApiService.GetResponse<TResponse>(request, cancellationToken);

      await HandleApiResponseAsync(apiResponse, cancellationToken);
    }
    catch (OperationCanceledException)
    {
      // Create SharedProblemDetails for cancellation and return it
      SharedProblemDetails sharedProblemDetails = new()
      {
        Title = "Operation Cancelled",
        Status = 499,// 499 is the code for "Client Closed Request"
        Detail = "The request was cancelled."
      };

      await HandleApiResponseAsync(sharedProblemDetails, cancellationToken);
    }
    catch (Exception ex)
    {
      // Log any unexpected exceptions
      Logger.LogError(ex, "Unexpected error occurred while handling {ActionType}", typeof(TAction).Name);
      throw;
    }
  }

  private async Task<SemaphoreGuard> AcquireSemaphoreAsync(Type stateType, CancellationToken cancellationToken)
  {
    SemaphoreSlim? semaphore = Store.GetSemaphore(stateType);

    if (semaphore == null)
    {
      // State has been removed, no need to proceed
      return new SemaphoreGuard(null, false);
    }

    try
    {
      await semaphore.WaitAsync(cancellationToken);
      return new SemaphoreGuard(semaphore, true);
    }
    catch (OperationCanceledException)
    {
      // Operation was cancelled
      return new SemaphoreGuard(semaphore, false);
    }
  }

  private async Task ValidateRequestAsync(TRequest request)
  {
    if (Validator != null)
    {
      ValidationResult? result = await Validator.ValidateAsync(request);
      if (!result.IsValid)
      {
        throw new ValidationException(result.Errors);
      }
    }
  }

  private async Task HandleApiResponseAsync
  (
    OneOf<TResponse, FileResponse, SharedProblemDetails> apiResponse,
    CancellationToken cancellationToken
  )
  {
    await apiResponse.Match(
      response => HandleSuccess(response, cancellationToken),
      fileResponse => HandleFileResponse(fileResponse, cancellationToken),
      problemDetails => HandleError(problemDetails, cancellationToken)
    );
  }

  private readonly struct SemaphoreGuard : IDisposable
  {
    private readonly SemaphoreSlim? Semaphore;
    public bool Acquired { get; }

    public SemaphoreGuard(SemaphoreSlim? semaphore, bool acquired)
    {
      Semaphore = semaphore;
      Acquired = acquired;
    }

    public void Dispose()
    {
      if (Acquired && Semaphore is not null)
      {
        try
        {
          Semaphore.Release();
        }
        catch (ObjectDisposedException)
        {
          // Log or handle the case where the semaphore was disposed
          // This means the semaphore was already disposed by RemoveState.
          // swallow the exception
        }
      }
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
