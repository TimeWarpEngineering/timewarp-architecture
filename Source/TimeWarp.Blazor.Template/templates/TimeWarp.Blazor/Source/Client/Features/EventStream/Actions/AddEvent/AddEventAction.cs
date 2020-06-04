namespace TimeWarp.Blazor.Features.EventStreams
{
  using TimeWarp.Blazor.Features.Bases;

  internal partial class EventStreamState
  {
    public class AddEventAction : BaseAction
    {
      public string Message { get; set; }
    }
  }
}
