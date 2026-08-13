#region Purpose
// Test-only seeding of EventStreamState's event list.
#endregion

#region Design
// EventList is private and normally mutable only via the AddEventActionSet handler; tests need
// arbitrary starting states without dispatching actions, so this bypass exists but is
// gated by ThrowIfNotTestAssembly to keep production code on the action pipeline.
#endregion

namespace TimeWarp.Architecture.Features.EventStreams;

partial class EventStreamState
{
  /// <summary>
  /// Use in Tests ONLY, to initialize the State
  /// </summary>
  /// <param name="events"></param>
  public void Initialize(List<string> events)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    EventList = events;
  }
}
