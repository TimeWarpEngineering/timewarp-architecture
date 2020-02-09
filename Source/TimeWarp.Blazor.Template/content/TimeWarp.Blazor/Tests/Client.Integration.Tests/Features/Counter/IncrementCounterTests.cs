namespace TimeWarp.Blazor.Integration.Tests.Features.Counter
{
  using Microsoft.AspNetCore.Blazor.Hosting;
  using Shouldly;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.CounterFeature;
  using TimeWarp.Blazor.Integration.Tests.Infrastructure;
  using static TimeWarp.Blazor.CounterFeature.CounterState;

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
