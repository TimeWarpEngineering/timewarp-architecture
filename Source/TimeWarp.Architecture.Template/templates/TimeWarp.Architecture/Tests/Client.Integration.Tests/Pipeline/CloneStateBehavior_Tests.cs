namespace CloneStateBehavior
{
  using FluentAssertions;
  using System;
  using System.Threading.Tasks;
  using TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;
  using TimeWarp.Architecture.Features.Counters;
  using static TimeWarp.Architecture.Features.Counters.CounterState;

  public class Should : BaseTest
  {
    private CounterState CounterState => Store.GetState<CounterState>();

    public Should(TestClientApplication aWebAssemblyHost) : base(aWebAssemblyHost) { }

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
      CounterState.Guid.Should().NotBe(preActionGuid);
    }

    public async Task RollBackState_When_Exception()
    {
      // Arrange
      CounterState.Initialize(aCount: 22);
      Guid preActionGuid = CounterState.Guid;

      // Act
      var throwExceptionAction = new ThrowExceptionAction
      {
        Message = "Test Rollback of State"
      };

      await Send(throwExceptionAction);

      // Assert State was rolled back and thus Guid didn't change.
      CounterState.Guid.Equals(preActionGuid);
    }
  }
}
