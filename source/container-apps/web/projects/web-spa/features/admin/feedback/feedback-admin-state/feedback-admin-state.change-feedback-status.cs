#region Purpose
// ChangeFeedbackStatus action: moves the open item to another status.
#endregion

#region Design
// The response carries the updated item, which replaces Current. The success toast says whether
// the filer was emailed (only some statuses notify, and only filers with an email on file).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Feedback;

using TimeWarp.Architecture.Features.Feedback;
using static ChangeFeedbackStatus;

partial class FeedbackAdminState
{
  public static class ChangeFeedbackStatusActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(Guid feedbackItemId, FeedbackStatus status)
      {
        FeedbackItemId = feedbackItemId;
        Status = status;
      }

      public Guid FeedbackItemId { get; }
      public FeedbackStatus Status { get; }
    }

    internal class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher
    ) : DefaultApiHandler<Action, Command, Response>(store, webServerApiService, logger, publisher)
    {
      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken) =>
        Task.FromResult<Command?>(new Command { FeedbackItemId = action.FeedbackItemId, Status = action.Status });

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        FeedbackAdminState.Current = response.Item;
        string title = response.FilerEmailed ? "Status changed. The filer was emailed." : "Status changed.";
        return Publisher.Publish(new OutcomeNotification(MessageBarIntent.Success, title), cancellationToken);
      }
    }
  }
}
