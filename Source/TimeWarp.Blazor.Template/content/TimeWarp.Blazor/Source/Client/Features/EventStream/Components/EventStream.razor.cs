namespace TimeWarp.Blazor.Client.EventStreamFeature
{
  using System.Collections.Generic;
  using TimeWarp.Blazor.Client.BaseFeature;

  public partial class EventStream
  {
    public IReadOnlyList<string> Events => EventStreamState.Events;
  }
}
