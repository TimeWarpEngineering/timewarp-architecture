#region Purpose
// SetSort action: change the column and direction the admin feedback list is ordered by.
#endregion

#region Design
// Pure state edit (task 327). The grid's sort click lands here through the page, which then
// dispatches FetchFeedbackList; the server sorts the whole match set before paging. A sort
// change resets Page to 1, the same as a filter change, because row positions move.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Feedback;

using TimeWarp.Architecture.Features.Feedback;

partial class FeedbackAdminState
{
  public static class SetSortActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Action(FeedbackListSortColumn sortBy, bool sortDescending)
      {
        SortBy = sortBy;
        SortDescending = sortDescending;
      }

      public FeedbackListSortColumn SortBy { get; }
      public bool SortDescending { get; }
    }

    internal class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        FeedbackAdminState.SortBy = action.SortBy;
        FeedbackAdminState.SortDescending = action.SortDescending;
        FeedbackAdminState.Page = 1;
        return default;
      }
    }
  }
}
