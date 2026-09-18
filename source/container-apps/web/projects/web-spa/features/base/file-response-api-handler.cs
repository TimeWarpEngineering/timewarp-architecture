#region Purpose
// Base for state action handlers whose API call returns a downloadable file stream.
#endregion

#region Design
// Counterpart to DefaultApiHandler: TResponse is pinned to Stream and HandleSuccess
// throws by design because file endpoints deliver through the FileResponse branch,
// which derived handlers must implement.
// Errors publish ProblemDetailsNotification (TWS0002 — handlers never send actions).
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
    IPublisher<ClientPipeline> publisher,
    IValidator<TRequest>? validator = null,
    AuthenticationStateProvider? authenticationStateProvider = null
  ) : base(store, apiService, logger, publisher, validator, authenticationStateProvider)
  {
  }

  protected override Task HandleSuccess(Stream response, CancellationToken cancellationToken) => throw new NotImplementedException();

  protected override Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken) =>
    Publisher.Publish(new ProblemDetailsNotification(problemDetails), cancellationToken);
}
