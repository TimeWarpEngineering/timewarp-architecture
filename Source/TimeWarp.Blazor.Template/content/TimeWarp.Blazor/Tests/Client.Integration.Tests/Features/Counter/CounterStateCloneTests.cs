namespace TimeWarp.Blazor.Client.Integration.Tests.Features.Counter
{
  using AnyClone;
  using BlazorState;
  using Microsoft.Extensions.DependencyInjection;
  using Shouldly;
  using System;
  using TimeWarp.Blazor.Client.CounterFeature;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;

  internal class CounterStateCloneTests
  {
    private CounterState CounterState { get; set; }

    public CounterStateCloneTests(TestFixture aTestFixture)
    {
      IServiceProvider serviceProvider = aTestFixture.ServiceProvider;
      IStore store = serviceProvider.GetService<IStore>();
      CounterState = store.GetState<CounterState>();
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
