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
// Also the canonical [CatalogAction] exemplar: Description is one plain sentence, Permissions
// reuse PermissionIds (the SPA policy names), and the int amount is palette/agent-suppliable.
// Also the only JavaScript-dispatchable action (TimeWarp.State 12.0.0-beta.8 opt-in):
// Program.AllowJavaScriptDispatch allows it under JavaScriptAlias, the stable wire name
// counter.ts sends instead of the CLR type name, so renaming the type does not break the JS.
#endregion

namespace TimeWarp.Architecture.Features.Counters;

partial class CounterState
{
  public static class IncrementCounterActionSet
  {
    /// <summary>Wire name counter.ts passes to timeWarpState.DispatchRequest.</summary>
    public const string JavaScriptAlias = "Counter.Increment";

    [CatalogAction
    (
      Description = "Add an amount to the demo counter; a negative amount decrements it.",
      Permissions = [PermissionIds.DeveloperAccess],
      Visibility = ActionVisibility.Both
    )]
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
