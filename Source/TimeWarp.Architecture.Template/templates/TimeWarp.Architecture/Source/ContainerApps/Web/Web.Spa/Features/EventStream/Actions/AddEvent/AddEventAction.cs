namespace TimeWarp.Architecture.Features.EventStreams;
internal partial class EventStreamState
{
  public record AddEventAction : BaseAction
  {
    public string Message { get; set; }
  }
}
