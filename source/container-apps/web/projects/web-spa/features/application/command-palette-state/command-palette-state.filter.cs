#region Purpose
// Filter: re-rank the palette roster for the typed query.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class CommandPaletteState
{
  public static class FilterActionSet
  {
    public sealed class Action : IBaseAction
    {
      public string Query { get; }

      public Action(string query)
      {
        Query = query;
      }
    }

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        CommandPaletteState.Apply(action.Query);
        return ValueTask.CompletedTask;
      }
    }
  }
}
