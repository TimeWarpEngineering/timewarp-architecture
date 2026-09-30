#region Purpose
// MoveHighlight: step the highlighted palette row up or down (Up/Down keys), wrapping at the ends.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class CommandPaletteState
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
        int count = CommandPaletteState.Matches.Count;
        if (count > 0)
        {
          int current = Math.Max(CommandPaletteState.HighlightedIndex, 0);
          CommandPaletteState.HighlightedIndex = ((current + action.Delta) % count + count) % count;
        }

        return ValueTask.CompletedTask;
      }
    }
  }
}
