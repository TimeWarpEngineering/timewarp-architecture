namespace TimeWarp.Blazor.EventStreamFeature
{
  using System.Collections.Generic;

  public partial class EventStream
  {
    public IReadOnlyList<string> Events => EventStreamState.Events;
  }
}
