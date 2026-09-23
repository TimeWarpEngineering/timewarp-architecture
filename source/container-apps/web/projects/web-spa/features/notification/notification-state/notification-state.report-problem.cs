#region Purpose
// ReportProblem action: records a SharedProblemDetails as an Error bar in the shell region.
#endregion

#region Design
// Components that call an API service directly (ceremony pages, bootstrap choice) get a
// SharedProblemDetails back and must not format it themselves. This action applies the one
// shape (FromProblem: Title→Title, Detail→Body) so a page-reported problem and the same
// problem published by DefaultApiHandler dedupe into a single bar. OperationCancelled (499)
// is ignored — user-initiated cancellation is not an error.
#endregion

namespace TimeWarp.Architecture.Features;

partial class NotificationState
{
  public static class ReportProblemActionSet
  {
    public sealed class Action : IBaseAction
    {
      public SharedProblemDetails Problem { get; }

      public Action(SharedProblemDetails problem)
      {
        Problem = problem;
      }
    }

    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle
      (
        Action action,
        CancellationToken cancellationToken
      )
      {
        _ = cancellationToken;
        if (action.Problem.Status == Constants.OperationCancelled)
        {
          return ValueTask.CompletedTask;
        }

        (string title, string? body) = FromProblem(action.Problem);
        NotificationState.AddMessage(MessageBarIntent.Error, title, body, DateTimeOffset.UtcNow);
        return ValueTask.CompletedTask;
      }
    }
  }
}
