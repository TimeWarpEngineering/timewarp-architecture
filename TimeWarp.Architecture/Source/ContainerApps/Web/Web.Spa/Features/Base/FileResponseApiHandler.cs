namespace TimeWarp.Architecture;

internal abstract class FileResponseApiHandler<TAction, TRequest> : ApiHandler<TAction, TRequest, Stream>
  where TAction : IBaseAction
  where TRequest : IApiRequest
{
  private readonly ISender Sender;

  protected FileResponseApiHandler
  (
    IStore store,
    IApiService apiService,
    ISender sender,
    ILogger<FileResponseApiHandler<TAction, TRequest>> logger,
    IValidator<TRequest>? validator = null,
    AuthenticationStateProvider? authenticationStateProvider = null
  ) : base(store, apiService, logger, validator, authenticationStateProvider)
  {
    Sender = sender;
  }

  protected override Task HandleSuccess(Stream response, CancellationToken cancellationToken) => throw new NotImplementedException();

  protected override async Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken)
  {
    await ToastNotificationState.AddProblemDetails(problemDetails, cancellationToken);
  }
}
