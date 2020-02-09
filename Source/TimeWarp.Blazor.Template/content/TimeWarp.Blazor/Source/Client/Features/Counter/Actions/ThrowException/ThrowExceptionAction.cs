namespace TimeWarp.Blazor.CounterFeature
{
  using TimeWarp.Blazor.BaseFeature;

  internal partial class CounterState
  {
    public class ThrowExceptionAction : BaseAction
    {
      public string Message { get; set; }
    }
  }
}
