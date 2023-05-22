namespace TimeWarp.Architecture.Features.EventStreams.Spa;

[StateAccessMixin]
internal partial class EventStreamState : State<EventStreamState>
{
  private List<string> _Events { get; set; }

  public IReadOnlyList<string> Events => _Events.AsReadOnly();

  public EventStreamState()
  {
    _Events = new List<string>();
  }

  public override void Initialize() { }
}
