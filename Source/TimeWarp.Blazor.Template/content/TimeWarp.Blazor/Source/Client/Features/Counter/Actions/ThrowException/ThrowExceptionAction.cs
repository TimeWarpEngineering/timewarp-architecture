namespace TimeWarp.Blazor.Client.CounterFeature
{
  using TimeWarp.Blazor.Client.BaseFeature;

  internal partial class CounterState
  {
    public class ThrowExceptionAction : BaseAction
    {
      public string Message { get; set; }
    }
  }
}
