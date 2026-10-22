#region Purpose
// SetFilter and SetPage actions: change what the admin feedback list asks the server for.
#endregion

#region Design
// Pure state edits. The page dispatches FetchFeedbackList after either one. A filter change
// resets Page to 1 so a narrower result never lands on an empty page. Search is the list
// query's existing title filter. The action takes "" rather than null: the ActionSet method
// generator drops nullable annotations, so a null default would not compile. Blank becomes
// null on state; otherwise the trimmed text.
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
        FeedbackSeverity? severity = null,
        string search = "")
      {
        Status = status;
        Kind = kind;
        IncludeClosed = includeClosed;
        Severity = severity;
        Search = search;
      }

      public FeedbackStatus? Status { get; }
      public FeedbackKind? Kind { get; }
      public bool IncludeClosed { get; }
      public FeedbackSeverity? Severity { get; }
      public string Search { get; }
    }

    internal class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        FeedbackAdminState.Status = action.Status;
        FeedbackAdminState.Kind = action.Kind;
        FeedbackAdminState.IncludeClosed = action.IncludeClosed;
        FeedbackAdminState.Severity = action.Severity;
        FeedbackAdminState.Search = string.IsNullOrWhiteSpace(action.Search) ? null : action.Search.Trim();
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
