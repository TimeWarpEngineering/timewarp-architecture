namespace TimeWarp.Blazor.CounterFeature
{
  using TimeWarp.Blazor.BaseFeature;

  internal partial class CounterState
  {
    public class IncrementCounterAction : BaseAction
    {
      public int Amount { get; set; }
    }
  }
}
