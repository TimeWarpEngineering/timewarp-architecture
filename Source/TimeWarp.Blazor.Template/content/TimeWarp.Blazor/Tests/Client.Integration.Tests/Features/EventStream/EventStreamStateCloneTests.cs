namespace EventStreamState
{
  using AnyClone;
  using Shouldly;
  using System.Collections.Generic;
  using TimeWarp.Blazor.Features.EventStreams.Client;
  using TimeWarp.Blazor.Integration.Tests.Infrastructure.Client;

  public class Clone_Should : BaseTest
  {
    private EventStreamState EventStreamState => Store.GetState<EventStreamState>();

    public Clone_Should(ClientHost aWebAssemblyHost) : base(aWebAssemblyHost) { }

    public void Clone()
    {
      //Arrange
      var events = new List<string> { "Event 1", "Event 2", "Event 3" };
      EventStreamState.Initialize(events);

      //Act
      var clone = EventStreamState.Clone() as EventStreamState;

      //Assert
      EventStreamState.Events.Count.ShouldBe(clone.Events.Count);
      EventStreamState.Guid.ShouldNotBe(clone.Guid);
      EventStreamState.Events[0].ShouldBe(clone.Events[0]);
    }
  }
}
