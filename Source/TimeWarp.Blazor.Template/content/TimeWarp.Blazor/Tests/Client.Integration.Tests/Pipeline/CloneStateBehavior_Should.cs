namespace CloneStateBehavior
{
  using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
  using Shouldly;
  using System;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Counters.Client;
  using TimeWarp.Blazor.Integration.Tests.Infrastructure.Client;
  using static TimeWarp.Blazor.Features.Counters.Client.CounterState;

  public class Should : BaseTest
  {
    private CounterState CounterState => Store.GetState<CounterState>();

    public Should(ClientHost aWebAssemblyHost) : base(aWebAssemblyHost) { }

    public async Task CloneState()
    {
      //Arrange
      CounterState.Initialize(aCount: 15);
      Guid preActionGuid = CounterState.Guid;

      // Create request
      var incrementCounterRequest = new IncrementCounterAction
      {
        Amount = -2
      };
      //Act
      await Send(incrementCounterRequest);

      //Assert
      CounterState.Guid.ShouldNotBe(preActionGuid);
    }

    public async Task RollBackStateChangesAndThrow_Given_ExceptionOccurs()
    {
      // Arrange
      CounterState.Initialize(aCount: 22);
      Guid preActionGuid = CounterState.Guid;

      // Act
      var throwExceptionAction = new ThrowExceptionAction
      {
        Message = "Test Rollback of State"
      };

      Exception exception = await Shouldly.Should.ThrowAsync<Exception>(async () =>
      await Send(throwExceptionAction));

      // Assert
      exception.Message.ShouldBe(throwExceptionAction.Message);
      CounterState.Guid.Equals(preActionGuid);
    }
  }
}
