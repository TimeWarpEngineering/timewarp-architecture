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

    ThrowExceptionActionSet.Action action = new(message: "Test Rollback of State");

    await scope.Send(action);

    // State was rolled back and thus Guid didn't change.
    scope.Store.GetState<CounterState>().Guid.ShouldBe(preActionGuid);

    // ExceptionNotificationHandler records a message bar with no FluentUI provider in the tree.
    TimeWarp.Architecture.Features.NotificationState toast =
      scope.Store.GetState<TimeWarp.Architecture.Features.NotificationState>();
    toast.Messages.Count.ShouldBe(1);
    toast.Messages[0].Title.ShouldBe("Test Rollback of State");
    toast.Messages[0].Intent.ShouldBe(MessageBarIntent.Error);
  }

  public static async Task AddNotification_Records_MessageBar_Without_Provider()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);

    await scope.Send
    (
      new TimeWarp.Architecture.Features.NotificationState.AddNotificationActionSet.Action
      (
        MessageBarIntent.Success,
        "Saved"
      )
    );

    TimeWarp.Architecture.Features.NotificationState toast =
      scope.Store.GetState<TimeWarp.Architecture.Features.NotificationState>();
    toast.Messages.Count.ShouldBe(1);
    toast.Messages[0].Title.ShouldBe("Saved");
    toast.Messages[0].Intent.ShouldBe(MessageBarIntent.Success);
  }
}
