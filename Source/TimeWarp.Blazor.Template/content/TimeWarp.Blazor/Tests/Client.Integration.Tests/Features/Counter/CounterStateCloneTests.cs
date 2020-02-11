namespace TimeWarp.Blazor.Features.Counters.Client.Tests
{
  using AnyClone;
  using Microsoft.AspNetCore.Blazor.Hosting;
  using Shouldly;
  using TimeWarp.Blazor.Features.Counters.Client;
  using TimeWarp.Blazor.Integration.Tests.Infrastructure.Client;

  internal class CounterStateCloneTests : BaseTest
  {
    private CounterState CounterState { get; set; }

    public CounterStateCloneTests(WebAssemblyHost aWebAssemblyHost) : base(aWebAssemblyHost)
    {
      CounterState = Store.GetState<CounterState>();
    }

    public void ShouldClone()
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
