#region Purpose
// SetFilter and SetPage actions: change what the admin feedback list asks the server for.
#endregion

#region Design
// Pure state edits. The page dispatches FetchFeedbackList after either one. A filter change
// resets Page to 1 so a narrower result never lands on an empty page.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Feedback;

using TimeWarp.Architecture.Features.Feedback;

partial class FeedbackAdminState
{
  public static class SetFilterActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Action(
        FeedbackStatus? status,
        FeedbackKind? kind,
        bool includeClosed,
        FeedbackSeverity? severity = null)
      {
        Status = status;
        Kind = kind;
        IncludeClosed = includeClosed;
        Severity = severity;
      }

      public FeedbackStatus? Status { get; }
      public FeedbackKind? Kind { get; }
      public bool IncludeClosed { get; }
      public FeedbackSeverity? Severity { get; }
    }

    internal class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        FeedbackAdminState.Status = action.Status;
        FeedbackAdminState.Kind = action.Kind;
        FeedbackAdminState.IncludeClosed = action.IncludeClosed;
        FeedbackAdminState.Severity = action.Severity;
        FeedbackAdminState.Page = 1;
        return default;
      }
    }
  }

  public static class SetPageActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Action(int page)
      {
        Page = page;
      }

      public int Page { get; }
    }

    internal class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        FeedbackAdminState.Page = Math.Max(1, action.Page);
        return default;
      }
    }
  }
}
