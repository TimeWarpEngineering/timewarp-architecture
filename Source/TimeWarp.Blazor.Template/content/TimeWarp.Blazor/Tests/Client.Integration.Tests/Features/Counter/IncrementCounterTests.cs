namespace TimeWarp.Blazor.Features.Counters.Client.Tests
{
  using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
  using Shouldly;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Counters.Client;
  using TimeWarp.Blazor.Integration.Tests.Infrastructure.Client;
  using static TimeWarp.Blazor.Features.Counters.Client.CounterState;

  internal class IncrementCounterTests : BaseTest
  {
    private CounterState CounterState => Store.GetState<CounterState>();

    public IncrementCounterTests(WebAssemblyHost aWebAssemblyHost) : base(aWebAssemblyHost) { }

    public async Task Should_Decrement_Counter()
    {
      //Arrange 
      CounterState.Initialize(aCount: 15);

      var incrementCounterRequest = new IncrementCounterAction
      {
        Amount = -2
      };

      //Act
      await Send(incrementCounterRequest);

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
      await Send(incrementCounterRequest);

      //Assert
      CounterState.Count.ShouldBe(27);
    }
  }
}
