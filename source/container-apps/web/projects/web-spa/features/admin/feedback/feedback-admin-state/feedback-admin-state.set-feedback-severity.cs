#region Purpose
// SetFeedbackSeverity action: records the open item's review severity.
#endregion

#region Design
// The response carries the updated item, which replaces Current. A review sends no email,
// so the toast does not mention the filer. The page dispatches this action; it does not call
// the endpoint itself.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Feedback;

using TimeWarp.Architecture.Features.Feedback;
using static SetFeedbackSeverity;

partial class FeedbackAdminState
{
  public static class SetFeedbackSeverityActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(Guid feedbackItemId, FeedbackSeverity severity)
      {
        FeedbackItemId = feedbackItemId;
        Severity = severity;
      }

      public Guid FeedbackItemId { get; }
      public FeedbackSeverity Severity { get; }
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
        Task.FromResult<Command?>(new Command { FeedbackItemId = action.FeedbackItemId, Severity = action.Severity });

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        FeedbackAdminState.Current = response.Item;
        return Publisher.Publish(new OutcomeNotification(MessageBarIntent.Success, "Severity set."), cancellationToken);
      }
    }
  }
}
