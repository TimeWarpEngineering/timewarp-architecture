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
// by default. That is a deliberate Counters → Analytics edge (CrossSliceReference).
#endregion

namespace TimeWarp.Architecture.Features.Counters;

using TimeWarp.Architecture.Features.Analytics;

partial class CounterState
{
  public static class IncrementCounterActionSet
  {
    [TrackEvent]
    [CrossSliceReference(typeof(AnalyticsState), "Demo IncrementCounter is the template's opted-in analytics exemplar.")]
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

      public override Task Handle
      (
        Action action,
        CancellationToken cancellationToken
      )
      {
        CounterState.Count += action.Amount;
        return Task.CompletedTask;
      }
    }
  }
}
