#region Purpose
// Accumulates the action names captured by the event-stream middleware demo for display on the EventStream page.
#endregion

namespace TimeWarp.Architecture.Features.EventStreams;

[StateAccess]
internal sealed partial class EventStreamState : State<EventStreamState>
{
  private List<string> EventList { get; set; } = [];

  public IReadOnlyList<string> Events => EventList.AsReadOnly();

  public override void Initialize() { }
}
