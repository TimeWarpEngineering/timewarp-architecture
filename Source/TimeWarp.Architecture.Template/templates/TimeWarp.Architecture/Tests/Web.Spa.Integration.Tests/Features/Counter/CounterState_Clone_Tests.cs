namespace CounterState_;

using AnyClone;
using FluentAssertions;
using TimeWarp.Architecture.Features.Counters;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

public class Clone_Should : BaseTest
{
  private CounterState CounterState => Store.GetState<CounterState>();

  public Clone_Should(SpaTestApplication aSpaTestApplication) : base(aSpaTestApplication) { }

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
