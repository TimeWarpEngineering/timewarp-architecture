namespace EventStreamState;

using AnyClone;
using FluentAssertions;
using System.Collections.Generic;
using TimeWarp.Architecture.Features.EventStreams;
using TimeWarp.Architecture.Testing;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

public class Clone_Should : BaseTest
{
  private EventStreamState EventStreamState => Store.GetState<EventStreamState>();

  public Clone_Should
  (
    SpaTestApplication<YarpServerApplication, TimeWarp.Architecture.Yarp.Server.Program> aSpaTestApplication
  ) : base(aSpaTestApplication) { }

  public void Clone()
  {
    //Arrange
    var events = new List<string> { "Event 1", "Event 2", "Event 3" };
    EventStreamState.Initialize(events);

    //Act
    EventStreamState clone = EventStreamState.Clone();

    //Assert
    EventStreamState.Events.Count.Should().Be(clone.Events.Count);
    EventStreamState.Guid.Should().NotBe(clone.Guid);
    EventStreamState.Events[0].Should().Be(clone.Events[0]);
  }
}
