namespace TimeWarp.Blazor.Features.EventStreams.Client
{
  using TimeWarp.Blazor.Features.Bases.Client;

  internal partial class EventStreamState
  {
    public class AddEventAction : BaseAction
    {
      public string Message { get; set; }
    }
  }
}
