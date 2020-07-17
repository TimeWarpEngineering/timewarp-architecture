namespace CounterState
{
  using AnyClone;
  using Shouldly;
  using TimeWarp.Blazor.Features.Counters;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;

  public class Clone_Should : BaseTest
  {
    private CounterState CounterState => Store.GetState<CounterState>();

    public Clone_Should(ClientHost aWebAssemblyHost) : base(aWebAssemblyHost) { }

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
