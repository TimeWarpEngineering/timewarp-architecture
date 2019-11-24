namespace TimeWarp.Blazor.Client.CounterFeature
{
  using TimeWarp.Blazor.Client.BaseFeature;

  internal partial class CounterState
  {
    public class IncrementCounterAction : BaseAction
    {
      public int Amount { get; set; }
    }
  }
}