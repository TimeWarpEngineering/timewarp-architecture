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
    IValidator<TRequest>? validator = null,
    AuthenticationStateProvider? authenticationStateProvider = null
  ) : base(store, apiService, validator, authenticationStateProvider)
  {
    Sender = sender;
  }

  protected override Task HandleSuccess(Stream response, CancellationToken cancellationToken) => throw new NotImplementedException();

  protected override async Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken)
  {
    // Send a toast notification when an error occurs
    await Sender.Send(new ToastNotificationState.AddProblemDetails.Action(problemDetails), cancellationToken);
  }
}
