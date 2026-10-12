#region Purpose
// ReplyToFeedback action: sends an admin reply the filer can see.
#endregion

#region Design
// LastReplySucceeded is false at request start and true in HandleSuccess so the page clears the
// reply draft only after the write landed. The response item replaces Current.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Feedback;

using static ReplyToFeedback;

partial class FeedbackAdminState
{
  public static class ReplyToFeedbackActionSet
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
        FeedbackAdminState.LastReplySucceeded = false;
        return Task.FromResult<Command?>(new Command { FeedbackItemId = action.FeedbackItemId, Body = action.Body });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        FeedbackAdminState.Current = response.Item;
        FeedbackAdminState.LastReplySucceeded = true;
        string title = response.FilerEmailed ? "Reply sent. The filer was emailed." : "Reply sent.";
        return Publisher.Publish(new OutcomeNotification(MessageBarIntent.Success, title), cancellationToken);
      }
    }
  }
}
