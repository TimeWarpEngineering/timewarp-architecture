namespace TimeWarp.Architecture.Features.EventStreams;

partial class EventStreamState
{
  /// <summary>
  /// Use in Tests ONLY, to initialize the State
  /// </summary>
  /// <param name="aEvents"></param>
  public void Initialize(List<string> aEvents)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    EventList = aEvents;
  }
}
