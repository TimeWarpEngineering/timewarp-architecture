namespace TimeWarp.Blazor.Client.EventStreamFeature
{
  using TimeWarp.Blazor.Client.BaseFeature;

  internal partial class EventStreamState
  {
    public class AddEventAction : BaseAction
    {
      public string Message { get; set; }
    }
  }
}
