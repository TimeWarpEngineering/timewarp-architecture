namespace CounterState
{
  using AnyClone;
  using FluentAssertions;
  using TimeWarp.Architecture.Client.Integration.Tests.Infrastructure;
  using TimeWarp.Architecture.Features.Counters;

  public class Clone_Should : BaseTest
  {
    private CounterState CounterState => Store.GetState<CounterState>();

    public Clone_Should(TestClientApplication aWebAssemblyHost) : base(aWebAssemblyHost) { }

    public void Clone()
    {
      //Arrange
      CounterState.Initialize(aCount: 15);

      //Act
      var clone = CounterState.Clone() as CounterState;

      //Assert
      CounterState.Should().NotBeSameAs(clone);
      CounterState.Count.Should().Be(clone.Count);
      CounterState.Guid.Should().NotBe(clone.Guid);
    }
  }
}
