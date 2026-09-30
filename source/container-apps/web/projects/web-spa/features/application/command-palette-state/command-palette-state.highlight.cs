#region Purpose
// Highlight: point the palette highlight at a specific row (pointer hover), so a click runs what Enter would.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class CommandPaletteState
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
        if (action.Index >= 0 && action.Index < CommandPaletteState.Matches.Count)
        {
          CommandPaletteState.HighlightedIndex = action.Index;
        }

        return ValueTask.CompletedTask;
      }
    }
  }
}
