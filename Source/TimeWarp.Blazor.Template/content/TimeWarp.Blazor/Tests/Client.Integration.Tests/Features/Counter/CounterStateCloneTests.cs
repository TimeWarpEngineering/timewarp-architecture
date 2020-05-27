namespace CounterState
{
  using AnyClone;
  using Shouldly;
  using TimeWarp.Blazor.Features.Counters.Client;
  using TimeWarp.Blazor.Integration.Tests.Infrastructure.Client;

  public class Clone_Should : BaseTest
  {
    private CounterState CounterState { get; set; }

    public Clone_Should(ClientHost aWebAssemblyHost) : base(aWebAssemblyHost)
    {
      CounterState = Store.GetState<CounterState>();
    }

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
