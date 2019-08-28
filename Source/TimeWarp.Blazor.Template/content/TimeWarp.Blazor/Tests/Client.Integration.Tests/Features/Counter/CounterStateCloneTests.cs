namespace TimeWarp.Blazor.Client.Integration.Tests.Features.Counter
{
  using AnyClone;
  using TimeWarp.Blazor.Client.Features.Counter;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;
  using BlazorState;
  using Microsoft.Extensions.DependencyInjection;
  using Shouldly;
  using System;

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