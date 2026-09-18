#region Purpose
// ApproveActionSet: human approves a pending link then refreshes the list from the response status.
#endregion

#region Design
// COPIC: page calls AgentLinksState.Approve(linkId). After success, the matching row's Status is
// updated locally so a second click cannot race a stale Pending button.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

using static ApproveAgentHumanLink;

partial class AgentLinksState
{
  public static class ApproveActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
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
        ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher)
        : base(store, webServerApiService, logger, publisher)
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
