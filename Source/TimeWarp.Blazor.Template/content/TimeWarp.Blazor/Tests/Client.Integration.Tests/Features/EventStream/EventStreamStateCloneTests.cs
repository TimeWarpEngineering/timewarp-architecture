namespace TimeWarp.Blazor.Client.Integration.Tests.Features.EventStream
{
  using AnyClone;
  using TimeWarp.Blazor.Client.EventStreamFeature;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;
  using BlazorState;
  using Microsoft.Extensions.DependencyInjection;
  using Shouldly;
  using System;
  using System.Collections.Generic;

  internal class EventStreamCloneTests
  {
    private EventStreamState EventStreamState { get; set; }

    public EventStreamCloneTests(TestFixture aTestFixture)
    {
      IServiceProvider serviceProvider = aTestFixture.ServiceProvider;
      IStore store = serviceProvider.GetService<IStore>();
      EventStreamState = store.GetState<EventStreamState>();
    }

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