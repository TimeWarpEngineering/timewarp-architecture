namespace TimeWarp.Architecture.Features;

internal abstract class DefaultApiHandler<TAction, TRequest, TResponse> : ApiHandler<TAction, TRequest, TResponse>
  where TAction : IBaseAction
  where TRequest : IApiRequest
  where TResponse : class
{
  private readonly ISender Sender;

  protected DefaultApiHandler
  (
    IStore store,
    IApiService apiService,
    ISender sender,
    ILogger<DefaultApiHandler<TAction, TRequest, TResponse>> logger,
    IValidator<TRequest>? validator = null,
    AuthenticationStateProvider? authenticationStateProvider = null
  ) : base(store, apiService, logger, validator, authenticationStateProvider)
  {
    Sender = sender;
  }

  protected override Task HandleFileResponse(FileResponse fileResponse, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override async Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken)
  {
    await ToastNotificationState.AddProblemDetails(problemDetails, cancellationToken);
  }
}
