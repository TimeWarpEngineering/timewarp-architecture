#region Purpose
// NoteComposerActionSet: records whether the feedback form has a kind, a title, and a body.
#endregion

#region Design
// The draft command lives on FeedbackListPage, not in the store. Publishing the whole body on
// each keystroke would re-render the page from the store in a loop and would put the filer's
// text into page_context. The page dispatches this action only when the kind or the
// title/body presence changes, and clears it on dispose. It is not a [CatalogAction]: the
// agent must not call it. page_context reads the flags; it does not read the draft text.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

partial class FeedbackState
{
  public static class NoteComposerActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Action(bool active, string kind, bool hasTitle, bool hasBody)
      {
        Active = active;
        Kind = kind;
        HasTitle = hasTitle;
        HasBody = hasBody;
      }

      public bool Active { get; }

      public string Kind { get; }

      public bool HasTitle { get; }

      public bool HasBody { get; }
    }

    internal sealed class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        _ = cancellationToken;
        FeedbackState.ComposerActive = action.Active;
        FeedbackState.DraftKind = action.Kind;
        FeedbackState.DraftHasTitle = action.HasTitle;
        FeedbackState.DraftHasBody = action.HasBody;
        return default;
      }
    }
  }
}
