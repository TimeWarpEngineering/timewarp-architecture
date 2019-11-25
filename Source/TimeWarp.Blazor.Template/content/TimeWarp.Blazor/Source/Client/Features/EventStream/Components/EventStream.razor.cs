namespace TimeWarp.Blazor.Client.EventStreamFeature
{
  using System.Collections.Generic;

  public partial class EventStream
  {
    public IReadOnlyList<string> Events => EventStreamState.Events;
  }
}
