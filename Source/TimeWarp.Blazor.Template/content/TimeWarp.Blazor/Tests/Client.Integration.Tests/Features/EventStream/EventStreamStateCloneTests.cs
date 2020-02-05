namespace TimeWarp.Blazor.Client.Integration.Tests.Features.EventStream
{
  using AnyClone;
  using Microsoft.AspNetCore.Blazor.Hosting;
  using Shouldly;
  using System.Collections.Generic;
  using TimeWarp.Blazor.Client.EventStreamFeature;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;

  internal class EventStreamCloneTests : BaseTest
  {
    private EventStreamState EventStreamState => Store.GetState<EventStreamState>();

    public EventStreamCloneTests(WebAssemblyHost aWebAssemblyHost) : base(aWebAssemblyHost) { }

    public void ShouldClone()
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
