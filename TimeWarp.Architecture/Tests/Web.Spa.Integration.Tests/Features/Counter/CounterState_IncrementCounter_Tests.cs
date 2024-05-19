namespace CounterState_;

using static TimeWarp.Architecture.Features.Counters.CounterState;

public class IncrementCounter_Action_Should : BaseTest
{
  private CounterState CounterState => Store.GetState<CounterState>();

  public IncrementCounter_Action_Should
  (
    SpaTestApplication<YarpTestServerApplication, TimeWarp.Architecture.Yarp.Server.Program> aSpaTestApplication
  ) : base(aSpaTestApplication) { }

  public async Task Decrement_Count_Given_NegativeAmount()
  {
    //Arrange
    CounterState.Initialize(aCount: 15);

    var action = new IncrementCounter.Action(Amount: -2);

    //Act
    await Send(action);

    //Assert
    CounterState.Count.Should().Be(13);
  }

  public async Task Increment_Count()
  {
    //Arrange
    CounterState.Initialize(aCount: 22);

    var action = new IncrementCounter.Action(Amount: 5);

    //Act
    await Send(action);

    //Assert
    CounterState.Count.Should().Be(27);
  }
}
