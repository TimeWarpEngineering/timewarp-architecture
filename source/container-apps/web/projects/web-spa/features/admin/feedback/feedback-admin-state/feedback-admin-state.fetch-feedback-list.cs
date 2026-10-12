#region Purpose
// FetchFeedbackList action: loads one page of ListFeedback using the current filter.
#endregion

#region Design
// The request reads Status, Kind, IncludeClosed, and Page from state, so the action carries no
// parameters. [TrackAction] drives the Section spinner. Not a catalog action: admin actions in
// this app are page-bound human controls.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Feedback;

using static ListFeedback;

partial class FeedbackAdminState
{
  public static class FetchFeedbackListActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction;

    internal class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher
    ) : DefaultApiHandler<Action, Query, Response>(store, webServerApiService, logger, publisher)
    {
      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken) =>
        Task.FromResult<Query?>(new Query
        {
          Status = FeedbackAdminState.Status,
          Kind = FeedbackAdminState.Kind,
          IncludeClosed = FeedbackAdminState.IncludeClosed,
          Page = FeedbackAdminState.Page,
          PageSize = FeedbackAdminState.PageSize,
        });

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        FeedbackAdminState.ItemList = [.. response.Items];
        FeedbackAdminState.TotalCount = response.TotalCount;
        FeedbackAdminState.Page = response.Page;
        FeedbackAdminState.PageSize = response.PageSize;
        return Task.CompletedTask;
      }
    }
  }
}
