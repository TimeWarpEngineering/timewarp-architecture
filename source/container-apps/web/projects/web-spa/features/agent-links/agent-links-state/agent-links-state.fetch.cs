#region Purpose
// FetchActionSet: loads the caller's agent-human links via ListAgentHumanLinks.
#endregion

#region Design
// DefaultApiHandler; [TrackAction] drives the page loading indicator.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

using static ListAgentHumanLinks;

partial class AgentLinksState
{
  internal static class FetchActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction;

    internal sealed class Handler : DefaultApiHandler<Action, Query, Response>
    {
      public Handler(
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger)
        : base(store, webServerApiService, logger)
      {
      }

      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken) =>
        Task.FromResult<Query?>(new Query());

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        AgentLinksState.Items = response.Items;
        return Task.CompletedTask;
      }
    }
  }
}
