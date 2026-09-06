#region Purpose
// DenyActionSet: human denies a pending link then updates the cached row status.
#endregion

#region Design
// Mirror of ApproveActionSet with DenyAgentHumanLink.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

using static DenyAgentHumanLink;

partial class AgentLinksState
{
  internal static class DenyActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
    {
      public Guid LinkId { get; }

      public Action(Guid linkId)
      {
        LinkId = linkId;
      }
    }

    internal sealed class Handler : DefaultApiHandler<Action, Command, Response>
    {
      public Handler(
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger)
        : base(store, webServerApiService, logger)
      {
      }

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        Command command = new();
        command.LinkId = action.LinkId;
        return Task.FromResult<Command?>(command);
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        AgentLinksState.Items =
        [
          .. AgentLinksState.Items.Select(item =>
            item.LinkId == response.LinkId
              ? new ListAgentHumanLinks.LinkSummary(
                item.LinkId,
                item.AgentPrincipalId,
                item.HumanPrincipalId,
                response.Status)
              : item)
        ];
        return Task.CompletedTask;
      }
    }
  }
}
