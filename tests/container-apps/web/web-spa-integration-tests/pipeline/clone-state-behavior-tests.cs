#region Purpose
// StateTransactionBehavior clone-on-dispatch and rollback-on-exception via SPA mediator pipeline.
// Migrated from dead SpaTestApplication<Yarp> to AspireSpaTestApplication (task 145-006).
#endregion

#region Design
// Guid/Count assertions re-fetch state after Send (instance may be replaced on dispatch).
// Rollback keeps Guid equal to the pre-action snapshot when the exception path restores state.
#endregion

namespace CloneStateBehavior;

using static TimeWarp.Architecture.Features.Counters.CounterState;

[TestTag("Integration")]
public class Should
{
  private static SpaSessionFixture? Session;
  private static AspireSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should>();

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

  public static async Task CloneState()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);

    scope.Store.GetState<CounterState>().Initialize(count: 15);
    Guid preActionGuid = scope.Store.GetState<CounterState>().Guid;

    IncrementCounterActionSet.Action action = new(amount: -2);

    await scope.Send(action);

    scope.Store.GetState<CounterState>().Guid.ShouldNotBe(preActionGuid);
  }

  public static async Task RollBackState_When_Exception()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);

    scope.Store.GetState<CounterState>().Initialize(count: 22);
    Guid preActionGuid = scope.Store.GetState<CounterState>().Guid;

    ThrowException.Action action = new(Message: "Test Rollback of State");

    await scope.Send(action);

    // State was rolled back and thus Guid didn't change.
    scope.Store.GetState<CounterState>().Guid.ShouldBe(preActionGuid);
  }
}
