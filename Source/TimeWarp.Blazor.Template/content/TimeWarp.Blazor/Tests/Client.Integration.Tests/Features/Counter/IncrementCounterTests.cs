namespace TimeWarp.Blazor.Client.Integration.Tests.Features.Counter
{
  using BlazorState;
  using MediatR;
  using Microsoft.Extensions.DependencyInjection;
  using Shouldly;
  using System;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Client.Features.Counter;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;
  using static TimeWarp.Blazor.Client.Features.Counter.CounterState;

  internal class IncrementCounterTests
  {
    private readonly IMediator Mediator;
    private readonly IServiceProvider ServiceProvider;
    private readonly IStore Store;
    private CounterState CounterState => Store.GetState<CounterState>();

    public IncrementCounterTests(TestFixture aTestFixture)
    {
      ServiceProvider = aTestFixture.ServiceProvider;
      Mediator = ServiceProvider.GetService<IMediator>();
      Store = ServiceProvider.GetService<IStore>();
    }

    public async Task Should_Decrement_Counter()
    {
      //Arrange
      CounterState.Initialize(aCount: 15);

      var incrementCounterRequest = new IncrementCounterAction
      {
        Amount = -2
      };

      //Act
      _ = await Mediator.Send(incrementCounterRequest);

      //Assert
      CounterState.Count.ShouldBe(13);
    }

    public async Task Should_Increment_Counter()
    {
      //Arrange
      CounterState.Initialize(aCount: 22);

      var incrementCounterRequest = new IncrementCounterAction
      {
        Amount = 5
      };

      //Act
      _ = await Mediator.Send(incrementCounterRequest);

      //Assert
      CounterState.Count.ShouldBe(27);
    }
  }
}