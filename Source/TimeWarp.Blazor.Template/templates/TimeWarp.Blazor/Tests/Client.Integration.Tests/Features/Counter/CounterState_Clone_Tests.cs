namespace CounterState
{
  using AnyClone;
  using Shouldly;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;
  using TimeWarp.Blazor.Features.Counters;

  public class Clone_Should : BaseTest
  {
    private CounterState CounterState => Store.GetState<CounterState>();

    public Clone_Should(TestClientApplication aWebAssemblyHost) : base(aWebAssemblyHost) { }

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
}
