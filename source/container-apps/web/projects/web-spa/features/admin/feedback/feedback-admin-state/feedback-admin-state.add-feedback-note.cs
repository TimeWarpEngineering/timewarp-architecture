#region Purpose
// AddFeedbackNote action: records an internal note the filer never sees.
#endregion

#region Design
// LastNoteSucceeded follows the same rule as LastReplySucceeded. The response item replaces
// Current.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Feedback;

using static AddFeedbackNote;

partial class FeedbackAdminState
{
  public static class AddFeedbackNoteActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(Guid feedbackItemId, string body)
      {
        FeedbackItemId = feedbackItemId;
        Body = body;
      }

      public Guid FeedbackItemId { get; }
      public string Body { get; }
    }

    internal class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher
    ) : DefaultApiHandler<Action, Command, Response>(store, webServerApiService, logger, publisher)
    {
      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        FeedbackAdminState.LastNoteSucceeded = false;
        return Task.FromResult<Command?>(new Command { FeedbackItemId = action.FeedbackItemId, Body = action.Body });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        FeedbackAdminState.Current = response.Item;
        FeedbackAdminState.LastNoteSucceeded = true;
        return Publisher.Publish(new OutcomeNotification(MessageBarIntent.Success, "Note added."), cancellationToken);
      }
    }
  }
}
