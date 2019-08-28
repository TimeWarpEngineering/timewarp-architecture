namespace TimeWarp.Blazor.Client.Features.EventStream.Components
{
  using System.Collections.Generic;
  using TimeWarp.Blazor.Client.Features.Base.Components;

  public class EventStreamBase : BaseComponent
  {
    public IReadOnlyList<string> Events => EventStreamState.Events;
  }
}
