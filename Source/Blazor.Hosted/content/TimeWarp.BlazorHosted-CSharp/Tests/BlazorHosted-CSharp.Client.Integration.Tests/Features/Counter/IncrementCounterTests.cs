namespace BlazorHosted_CSharp.Client.Integration.Tests.Features.Counter
{
  using System;
  using System.Threading.Tasks;
  using BlazorHosted_CSharp.Client.Features.Counter;
  using BlazorState;
  using BlazorHosted_CSharp.Client.Integration.Tests.Infrastructure;
  using MediatR;
  using Microsoft.Extensions.DependencyInjection;
  using Shouldly;

  internal class IncrementCounterTests
  {
    public IncrementCounterTests(TestFixture aTestFixture)
    {
      ServiceProvider = aTestFixture.ServiceProvider;
      Mediator = ServiceProvider.GetService<IMediator>();
      Store = ServiceProvider.GetService<IStore>();
      CounterState = Store.GetState<CounterState>();
    }

    private CounterState CounterState { get; set; }
    private IMediator Mediator { get; }
    private IServiceProvider ServiceProvider { get; }
    private IStore Store { get; }

    public async Task Should_Decrement_Counter()
    {
      CounterState.Initialize(aCount: 15);

      var incrementCounterRequest = new IncrementCounterAction
      {
        Amount = -2
      };

      CounterState = await Mediator.Send(incrementCounterRequest);

      CounterState.Count.ShouldBe(13);
    }

    public async Task Should_Increment_Counter()
    {
      CounterState.Initialize(aCount: 22);

      var incrementCounterRequest = new IncrementCounterAction
      {
        Amount = 5
      };

      CounterState = await Mediator.Send(incrementCounterRequest);

      CounterState.Count.ShouldBe(27);
    }
  }
}
