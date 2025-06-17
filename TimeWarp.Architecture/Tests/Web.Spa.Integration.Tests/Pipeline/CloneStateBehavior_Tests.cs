namespace CloneStateBehavior;

using static TimeWarp.Architecture.Features.Counters.CounterState;

public class Should : BaseTest
{
  private CounterState CounterState => Store.GetState<CounterState>();

  public Should
  (
    SpaTestApplication<YarpTestServerApplication, TimeWarp.Architecture.Yarp.Server.Program> aSpaTestApplication
  ) : base(aSpaTestApplication) { }

  public async Task CloneState()
  {
    //Arrange
    CounterState.Initialize(aCount: 15);
    Guid preActionGuid = CounterState.Guid;

    var action = new IncrementCounterActionSet.Action(amount: -2);

    //Act
    await Send(action);

    //Assert
    CounterState.Guid.Should().NotBe(preActionGuid);
  }

  public async Task RollBackState_When_Exception()
  {
    // Arrange
    CounterState.Initialize(aCount: 22);
    Guid preActionGuid = CounterState.Guid;

    // Act
    var action =
      new ThrowException.Action(Message: "Test Rollback of State");

    await Send(action);

    // Assert State was rolled back and thus Guid didn't change.
    CounterState.Guid.Equals(preActionGuid);
  }
}
