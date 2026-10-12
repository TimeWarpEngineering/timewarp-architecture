#region Purpose
// OpenFeedbackForAdmin action: loads one item with its full thread, internal notes included.
#endregion

#region Design
// A 404 clears Current and sets a non-revealing LoadError, then still publishes the problem
// toast. Opening a different item first clears Current and LoadError, so the previous item's
// body and manage controls never show under the new id while it loads or after it fails.
// Every open clears the draft success flags.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Feedback;

using static GetFeedbackForAdmin;

partial class FeedbackAdminState
{
  public static class OpenFeedbackForAdminActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(Guid feedbackItemId)
      {
        FeedbackItemId = feedbackItemId;
      }

      public Guid FeedbackItemId { get; }
    }

    internal class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher
    ) : DefaultApiHandler<Action, Query, Response>(store, webServerApiService, logger, publisher)
    {
      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        if (FeedbackAdminState.Current?.FeedbackItemId != action.FeedbackItemId)
        {
          FeedbackAdminState.Current = null;
          FeedbackAdminState.LoadError = null;
        }

        FeedbackAdminState.LastReplySucceeded = false;
        FeedbackAdminState.LastNoteSucceeded = false;
        return Task.FromResult<Query?>(new Query { FeedbackItemId = action.FeedbackItemId });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        FeedbackAdminState.Current = response.Item;
        FeedbackAdminState.LoadError = null;
        return Task.CompletedTask;
      }

      protected override async Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken)
      {
        if (problemDetails.Status == 404)
        {
          FeedbackAdminState.Current = null;
          FeedbackAdminState.LoadError = "This feedback item is not available.";
        }

        await base.HandleError(problemDetails, cancellationToken);
      }
    }
  }
}
