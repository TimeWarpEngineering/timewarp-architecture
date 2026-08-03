#region Purpose
// CounterState IncrementCounter action happy paths against AspireSpaTestApplication.
#endregion

#region Design
// Re-fetch CounterState via Store.GetState after Send — StateTransactionBehavior (or equivalent)
// replaces the state instance on dispatch; a pre-Send local reference is stale.
#endregion

namespace CounterState_;

using static TimeWarp.Architecture.Features.Counters.CounterState;

[TestTag("Integration")]
public class IncrementCounter_Action_Should
{
  private static SpaSessionFixture? Session;
  private static AspireSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IncrementCounter_Action_Should>();

  public static async Task SetupOnce()
  {
    Session = await SessionFixture.GetAsync<SpaSessionFixture>();
    Spa = new AspireSpaTestApplication(Session.Inner);
  }

  public static Task CleanUpOnce()
  {
    // Session-owned: the Jaribu session hook disposes SpaSessionFixture; do not dispose here.
    Session = null;
    Spa = null;
    return Task.CompletedTask;
  }

  public static async Task Decrement_Count_Given_NegativeAmount()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);

    scope.Store.GetState<CounterState>().Initialize(count: 15);
    IncrementCounterActionSet.Action action = new(amount: -2);

    await scope.Send(action);

    scope.Store.GetState<CounterState>().Count.ShouldBe(13);
  }

  public static async Task Increment_Count()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);

    scope.Store.GetState<CounterState>().Initialize(count: 22);
    IncrementCounterActionSet.Action action = new(amount: 5);

    await scope.Send(action);

    scope.Store.GetState<CounterState>().Count.ShouldBe(27);
  }
}
