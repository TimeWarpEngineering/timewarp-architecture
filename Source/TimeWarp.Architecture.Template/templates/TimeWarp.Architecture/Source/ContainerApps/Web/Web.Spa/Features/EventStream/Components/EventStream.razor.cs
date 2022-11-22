namespace TimeWarp.Architecture.Features.EventStreams;

public partial class EventStream
{
  public IReadOnlyList<string> Events => EventStreamState.Events;
}
