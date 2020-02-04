namespace TimeWarp.Blazor.Client.Integration.Tests.Features.Counter
{
  using AnyClone;
  using Microsoft.AspNetCore.Blazor.Hosting;
  using Shouldly;
  using TimeWarp.Blazor.Client.CounterFeature;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;

  internal class CounterStateCloneTests : BaseTest
  {
    private CounterState CounterState { get; set; }

    public CounterStateCloneTests(IWebAssemblyHost aWebAssemblyHost) : base(aWebAssemblyHost)
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
