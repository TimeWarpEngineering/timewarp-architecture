#region Purpose
// Base for state action handlers whose API call returns a JSON body.
#endregion

#region Design
// Closes ApiHandler's OneOf branches for the common case: errors publish
// ProblemDetailsNotification (TWS0002 — handlers never send actions). Feature
// handlers implement only GetRequest and HandleSuccess.
// HandleFileResponse throws by design — JSON endpoints never return files; derive
// from FileResponseApiHandler for downloads instead.
#endregion

namespace TimeWarp.Architecture.Features;

internal abstract class DefaultApiHandler<TAction, TRequest, TResponse> : ApiHandler<TAction, TRequest, TResponse>
  where TAction : IBaseAction
  where TRequest : IApiRequest
  where TResponse : class
{
  protected DefaultApiHandler
  (
    IStore store,
    IApiService apiService,
    ILogger<DefaultApiHandler<TAction, TRequest, TResponse>> logger,
    IPublisher<ClientPipeline> publisher,
    IValidator<TRequest>? validator = null,
    AuthenticationStateProvider? authenticationStateProvider = null
  ) : base(store, apiService, logger, publisher, validator, authenticationStateProvider)
  {
  }

  protected override Task HandleFileResponse(FileResponse fileResponse, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken) =>
    Publisher.Publish(new ProblemDetailsNotification(problemDetails), cancellationToken);
}
