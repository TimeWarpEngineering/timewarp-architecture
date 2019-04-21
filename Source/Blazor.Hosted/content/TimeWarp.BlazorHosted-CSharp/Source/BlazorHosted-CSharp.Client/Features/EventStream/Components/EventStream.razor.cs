namespace BlazorHosted_CSharp.Client.Features.EventStream.Components
{
  using System.Collections.Generic;
  using BlazorHosted_CSharp.Client.Features.Base.Components;

  public class EventStreamModel : BaseComponent
  {
    public List<string> Events => GetState<EventStreamState>().Events;

  }
}
