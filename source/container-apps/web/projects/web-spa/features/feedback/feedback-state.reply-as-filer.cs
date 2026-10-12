#region Purpose
// ReplyAsFilerActionSet: the filer answers on their own item.
#endregion

#region Design
// Visibility Human: the reply box on the permalink page dispatches it. Not on the read-only
// allow-list. The response carries the new Status and the filer-visible Activity, which replace
// CurrentStatus and CurrentActivity (Current is immutable). LastReplySucceeded is false at request
// start and true in HandleSuccess so the page clears its draft only after the write landed.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

using static ReplyAsFiler;

partial class FeedbackState
{
  public static class ReplyAsFilerActionSet
  {
    [CatalogAction
    (
      Description = "Reply on one of your feedback filings.",
      Permissions = [PermissionIds.FeedbackFileSelf],
      Visibility = ActionVisibility.Human
    )]
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

    internal sealed class Handler : DefaultApiHandler<Action, Command, Response>
    {
      public Handler
      (
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
        IPublisher<ClientPipeline> publisher,
        IValidator<Command>? validator = null,
        AuthenticationStateProvider? authenticationStateProvider = null
      ) : base(store, webServerApiService, logger, publisher, validator, authenticationStateProvider)
      {
      }

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        FeedbackState.LastReplySucceeded = false;
        return Task.FromResult<Command?>(new Command { FeedbackItemId = action.FeedbackItemId, Body = action.Body });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        FeedbackState.CurrentStatus = response.Status;
        FeedbackState.CurrentActivity = response.Activity;
        FeedbackState.LastReplySucceeded = true;
        return Publisher.Publish(new OutcomeNotification(MessageBarIntent.Success, "Reply sent."), cancellationToken);
      }
    }
  }
}
