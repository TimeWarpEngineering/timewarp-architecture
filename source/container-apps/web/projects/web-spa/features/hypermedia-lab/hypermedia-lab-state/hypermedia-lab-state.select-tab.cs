#region Purpose
// SelectTab: switch the hypermedia lab between approach B and approach C.
#endregion

namespace TimeWarp.Architecture.Features.HypermediaLab;

partial class HypermediaLabState
{
  public static class SelectTabActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Action(string tab)
      {
        Tab = tab;
      }

      public string Tab { get; }
    }

    internal sealed class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        if (action.Tab is ApproachB or ApproachC)
        {
          HypermediaLabState.SelectedTab = action.Tab;
        }

        return ValueTask.CompletedTask;
      }
    }
  }
}
