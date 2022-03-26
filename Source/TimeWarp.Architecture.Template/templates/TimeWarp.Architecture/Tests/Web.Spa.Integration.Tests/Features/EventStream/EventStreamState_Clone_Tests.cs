namespace EventStreamState;

using AnyClone;
using FluentAssertions;
using System.Collections.Generic;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;
using TimeWarp.Architecture.Features.EventStreams;

public class Clone_Should : BaseTest
{
  private EventStreamState EventStreamState => Store.GetState<EventStreamState>();

  public Clone_Should(TestClientApplication aWebAssemblyHost) : base(aWebAssemblyHost) { }

  public void Clone()
  {
    //Arrange
    var events = new List<string> { "Event 1", "Event 2", "Event 3" };
    EventStreamState.Initialize(events);

    //Act
    var clone = EventStreamState.Clone() as EventStreamState;

    //Assert
    EventStreamState.Events.Count.Should().Be(clone.Events.Count);
    EventStreamState.Guid.Should().NotBe(clone.Guid);
    EventStreamState.Events[0].Should().Be(clone.Events[0]);
  }
}
