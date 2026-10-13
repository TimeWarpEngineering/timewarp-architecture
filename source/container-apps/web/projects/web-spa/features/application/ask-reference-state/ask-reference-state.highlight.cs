#region Purpose
// Highlight: highlight one picker row (mouse hover).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AskReferenceState
{
  public static class HighlightActionSet
  {
    public sealed class Action : IBaseAction
    {
      public int Index { get; }

      public Action(int index)
      {
        Index = index;
      }
    }

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        if (action.Index >= 0 && action.Index < AskReferenceState.RowCount)
        {
          AskReferenceState.HighlightedIndex = action.Index;
        }

        return ValueTask.CompletedTask;
      }
    }
  }
}
