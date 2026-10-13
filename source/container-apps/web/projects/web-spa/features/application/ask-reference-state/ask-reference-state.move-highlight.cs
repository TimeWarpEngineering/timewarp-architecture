#region Purpose
// MoveHighlight: step the highlighted picker row up or down (Up/Down keys), wrapping at the ends.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AskReferenceState
{
  public static class MoveHighlightActionSet
  {
    public sealed class Action : IBaseAction
    {
      /// <summary>+1 for Down, -1 for Up.</summary>
      public int Delta { get; }

      public Action(int delta)
      {
        Delta = delta;
      }
    }

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        int count = AskReferenceState.RowCount;
        if (count > 0)
        {
          int current = Math.Max(AskReferenceState.HighlightedIndex, 0);
          AskReferenceState.HighlightedIndex = ((current + action.Delta) % count + count) % count;
        }

        return ValueTask.CompletedTask;
      }
    }
  }
}
