namespace CounterState_;

public class Clone_Should : BaseTest
{
  private CounterState CounterState => Store.GetState<CounterState>();

  public Clone_Should
  (
    ISpaTestApplication aSpaTestApplication
  ) : base(aSpaTestApplication) { }

  public void Clone()
  {
    //Arrange
    CounterState.Initialize(aCount: 15);

    //Act
    var clone = CounterState.Clone() as CounterState;

    //Assert
    CounterState.ShouldNotBeSameAs(clone);
    CounterState.Count.ShouldBe(clone.Count);
    CounterState.Guid.ShouldNotBe(clone.Guid);
  }
}
