#region Purpose
// Base for state action handlers whose API call returns a JSON body.
#endregion

#region Design
// Closes ApiHandler's OneOf branches for the common case: errors surface as toast
// notifications via ToastNotificationState, so feature handlers implement only
// GetRequest and HandleSuccess.
// HandleFileResponse throws by design — JSON endpoints never return files; derive
// from FileResponseApiHandler for downloads instead.
#endregion

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
