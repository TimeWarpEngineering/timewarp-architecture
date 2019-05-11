namespace BlazorHosted_CSharp.Client.Features.EventStream.Components
{
  using System.Collections.Generic;
  using BlazorHosted_CSharp.Client.Features.Base.Components;

  public class EventStreamModel : BaseComponent
  {
    public IReadOnlyList<string> Events => EventStreamState.Events;

  }
}
