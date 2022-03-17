namespace TimeWarp.Architecture.Features.EventStreams
{
  using TimeWarp.Architecture.Features.Bases;

  internal partial class EventStreamState
  {
    public class AddEventAction : BaseAction
    {
      public string Message { get; set; }
    }
  }
}
