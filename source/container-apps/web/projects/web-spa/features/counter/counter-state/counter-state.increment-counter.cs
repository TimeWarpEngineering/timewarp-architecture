#region Purpose
// CounterState action that adds an amount to Count.
#endregion

#region Design
// Canonical example of the ActionSet pattern (nested Action + Handler in a static
// class) that feature states copy.
// Amount rides on the action and may be negative, so one ActionSet covers both
// increment and decrement without a second handler.
// Tagged [TrackEvent] so the analytics pipeline-behavior exemplar fires on the demo
// increment — the one action the template opts in, rather than tracking every action
// by default. [TrackEvent] is Features substrate, so IncrementCounter opts in without
// a product→product edge.
#endregion

namespace TimeWarp.Architecture.Features.Counters;

partial class CounterState
{
  public static class IncrementCounterActionSet
  {
    [TrackEvent]
    public class Action : IBaseAction
    {
      public int Amount { get; }
      public Action(int amount)
      {
        Amount = amount;
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
        CounterState.Count += action.Amount;
        return default;
      }
    }
  }
}
