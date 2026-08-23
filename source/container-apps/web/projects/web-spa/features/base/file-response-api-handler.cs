#region Purpose
// Base for state action handlers whose API call returns a downloadable file stream.
#endregion

#region Design
// Counterpart to DefaultApiHandler: TResponse is pinned to Stream and HandleSuccess
// throws by design because file endpoints deliver through the FileResponse branch,
// which derived handlers must implement.
// Errors surface as toast notifications via ToastNotificationState.AddProblemDetails;
// the base does NOT hold/use ISender (TWA0022 defence in depth).
#endregion

namespace TimeWarp.Architecture;

internal abstract class FileResponseApiHandler<TAction, TRequest> : ApiHandler<TAction, TRequest, Stream>
  where TAction : IBaseAction
  where TRequest : IApiRequest
{
  protected FileResponseApiHandler
  (
    IStore store,
    IApiService apiService,
    ILogger<FileResponseApiHandler<TAction, TRequest>> logger,
    IValidator<TRequest>? validator = null,
    AuthenticationStateProvider? authenticationStateProvider = null
  ) : base(store, apiService, logger, validator, authenticationStateProvider)
  {
  }

  protected override Task HandleSuccess(Stream response, CancellationToken cancellationToken) => throw new NotImplementedException();

  protected override async Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken)
  {
    await ToastNotificationState.AddProblemDetails(problemDetails, cancellationToken);
  }
}
