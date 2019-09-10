namespace TimeWarp.Blazor.Client.Features.EventStream
{
  using TimeWarp.Blazor.Client.Features.Base;

  internal partial class EventStreamState
  {
    public class AddEventAction : BaseAction
    {
      public string Message { get; set; }
    }
  }
}