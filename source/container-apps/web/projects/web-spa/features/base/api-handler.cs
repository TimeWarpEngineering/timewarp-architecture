#region Purpose
// Template-method base for action handlers that call the Web API: build request, send, dispatch the OneOf result.
#endregion

#region Design
// A per-state semaphore serializes the request + network phase only
// (GetRequest → ValidateRequestAsync → ApiService.GetResponse). HandleApiResponseAsync
// (HandleSuccess / HandleError / HandleFileResponse) runs after release so an accidental
// nested dispatch on the same state waits instead of deadlocking. Handlers still must not
// dispatch another action (pages sequence); the narrowed lock makes a miss slow, not fatal.
// The semaphore is one per (store, state type), shared across every handler type through
// ApiHandlerSemaphores. Authentication is opt-in via the nullable
// AuthenticationStateProvider: when supplied, actions from anonymous users are silently
// dropped rather than surfacing a 401 round-trip. ConfirmAsync runs after that auth check
// and before AcquireSemaphoreAsync. The default answer is yes. A handler that must ask
// (ClearXaiApiKey) overrides it and calls IConfirmationService. No returns immediately:
// no GetRequest, no error notification, and the per-state lock is never taken, so a
// person answering cannot stall another action on the same state (a status fetch, for
// example). GetRequest returning null remains the sanctioned skip once the lock is held.
// Cancellation maps to a synthetic 499 SharedProblemDetails so HandleError stays the
// single failure path; validation failures throw instead, since they indicate a
// client-side coding bug.
#endregion

namespace TimeWarp.Architecture.Features;

internal abstract class ApiHandler<TAction, TRequest, TResponse> : BaseHandler<TAction>
  where TAction : IBaseAction
  where TRequest : IApiRequest
  where TResponse : class
{
  private readonly AuthenticationStateProvider? AuthenticationStateProvider;
  private readonly IApiService ApiService;
  private readonly ILogger<ApiHandler<TAction, TRequest, TResponse>> Logger;
  protected readonly IPublisher<ClientPipeline> Publisher;
  private readonly IValidator<TRequest>? Validator;
  private bool RequiresAuthentication => AuthenticationStateProvider is not null;

  protected ApiHandler
  (
    IStore store,
    IApiService apiService,
    ILogger<ApiHandler<TAction, TRequest, TResponse>> logger,
    IPublisher<ClientPipeline> publisher,
    IValidator<TRequest>? validator = null,
    AuthenticationStateProvider? authenticationStateProvider = null
  ) : base(store)
  {
    AuthenticationStateProvider = authenticationStateProvider;
    ApiService = apiService;
    Logger = logger;
    Publisher = publisher;
    Validator = validator;
  }

  public sealed override async ValueTask Handle(TAction action, CancellationToken cancellationToken)
  {
    if (RequiresAuthentication && !await IsUserAuthenticatedAsync()) return;

    if (!await ConfirmAsync(action, cancellationToken)) return;

    Type currentType = typeof(TAction).GetEnclosingStateType();

    OneOf<TResponse, FileResponse, SharedProblemDetails>? apiResponse = null;
    try
    {
      using SemaphoreGuard semaphoreGuard = await AcquireSemaphoreAsync(currentType, cancellationToken);
      if (!semaphoreGuard.Acquired) return;

      TRequest? request = await GetRequest(action, cancellationToken);
      if (request is null) return;// Skip the action

      await ValidateRequestAsync(request);

      apiResponse = await ApiService.GetResponse<TResponse>(request, cancellationToken);
    }
    catch (OperationCanceledException)
    {
      // Create SharedProblemDetails for cancellation and return it
      apiResponse = new SharedProblemDetails
      {
        Title = "Operation Cancelled",
        Status = 499,// 499 is the code for "Client Closed Request"
        Detail = "The request was cancelled."
      };
    }
    catch (Exception ex)
    {
      // Log any unexpected exceptions
      Logger.LogError(ex, "Unexpected error occurred while handling {ActionType}", typeof(TAction).Name);
      throw;
    }

    if (apiResponse is { } response)
    {
      await HandleApiResponseAsync(response, cancellationToken);
    }
  }

  private async Task<SemaphoreGuard> AcquireSemaphoreAsync(Type stateType, CancellationToken cancellationToken)
  {
    SemaphoreSlim semaphore = ApiHandlerSemaphores.Get(Store, stateType);

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
          // Defensive: ApiHandlerSemaphores never disposes its semaphores today.
        }
      }
    }
  }

  private async Task<bool> IsUserAuthenticatedAsync()
  {
    // Re-read on every handle. The handler does not keep an AuthenticationState across calls.
#pragma warning disable BL0013
    AuthenticationState authState = await AuthenticationStateProvider!.GetAuthenticationStateAsync();
#pragma warning restore BL0013
    ClaimsPrincipal user = authState.User;
    return user.Identity?.IsAuthenticated ?? false;
  }

  /// <summary>
  /// Asks before the per-state lock. False skips the action: no request and no error notification.
  /// </summary>
  protected virtual Task<bool> ConfirmAsync(TAction action, CancellationToken cancellationToken)
  {
    _ = action;
    _ = cancellationToken;
    return Task.FromResult(true);
  }

  /// <summary>
  /// Get the request object to send to the API
  /// If null is returned, the action will be skipped
  /// </summary>
  protected abstract Task<TRequest?> GetRequest(TAction action, CancellationToken cancellationToken);
  protected abstract Task HandleSuccess(TResponse response, CancellationToken cancellationToken);
  protected abstract Task HandleFileResponse(FileResponse fileResponse, CancellationToken cancellationToken);
  protected abstract Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken);
}
