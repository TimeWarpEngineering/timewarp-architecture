namespace TimeWarp.Blazor.Integration.Tests.Features.Counter
{
  using AnyClone;
  using Microsoft.AspNetCore.Blazor.Hosting;
  using Shouldly;
  using TimeWarp.Blazor.CounterFeature;
  using TimeWarp.Blazor.Integration.Tests.Infrastructure;

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
