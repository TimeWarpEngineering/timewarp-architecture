namespace TimeWarp.Architecture.Features.EventStreams;

[StateAccessMixin]
internal sealed partial class EventStreamState : State<EventStreamState>
{
  private List<string> EventList { get; set; } = [];

  public IReadOnlyList<string> Events => EventList.AsReadOnly();

  public override void Initialize() { }
}
