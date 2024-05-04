namespace TimeWarp.Architecture.Features;

internal abstract class DefaultFetchHandler<TAction, TResponse, TRequest> : FetchHandler<TAction, TResponse, TRequest>
  where TAction : IBaseAction
  where TResponse : class
  where TRequest : IApiRequest
{
  private readonly ISender Sender;

  protected DefaultFetchHandler
  (
    IStore store,
    IApiService apiService,
    ISender sender,
    AuthenticationStateProvider? authenticationStateProvider = null
  ) : base(store, apiService, authenticationStateProvider)
  {
    Sender = sender;
  }

  protected override async Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken)
  {
    // Send a toast notification when an error occurs
    await Sender.Send(new ToastNotificationState.AddProblemDetails.Action(problemDetails), cancellationToken);
  }
}
