namespace TimeWarp.Blazor.EventStreamFeature
{
  using TimeWarp.Blazor.BaseFeature;

  internal partial class EventStreamState
  {
    public class AddEventAction : BaseAction
    {
      public string Message { get; set; }
    }
  }
}
