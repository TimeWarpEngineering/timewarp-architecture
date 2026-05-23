namespace TimeWarp.Architecture.Features.EventStreams;

partial class EventStream
{
  public IReadOnlyList<string> Events => EventStreamState.Events;
}
