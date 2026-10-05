#region Purpose
// FetchCredentialCommands: loads approach C's payload (credentials + link commands) into HypermediaLabState.
#endregion

#region Design
// The initial load only; after that FollowCommand refreshes C itself by following the payload's
// Self link. Not cataloged — C needs no catalog follow-up.
#endregion

namespace TimeWarp.Architecture.Features.HypermediaLab;

using static GetCredentialCommands;

partial class HypermediaLabState
{
  public static class FetchCredentialCommandsActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction;

    internal sealed class Handler : DefaultApiHandler<Action, Query, Response>
    {
      public Handler
      (
        IStore store,
        TimeWarp.Architecture.Services.IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
        IPublisher<ClientPipeline> publisher,
        AuthenticationStateProvider authenticationStateProvider
      ) : base(store, webServerApiService, logger, publisher, authenticationStateProvider: authenticationStateProvider) { }

      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken) =>
        Task.FromResult<Query?>(new Query());

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        HypermediaLabState.Commands = response;
        return Task.CompletedTask;
      }
    }
  }
}
