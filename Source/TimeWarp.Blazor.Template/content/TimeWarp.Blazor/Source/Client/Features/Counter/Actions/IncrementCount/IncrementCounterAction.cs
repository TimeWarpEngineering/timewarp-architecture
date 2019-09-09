namespace TimeWarp.Blazor.Client.Features.Counter
{
  using TimeWarp.Blazor.Client.Features.Base;

  internal partial class CounterState
  {
    public class IncrementCounterAction : BaseAction
    {
      public int Amount { get; set; }
    }
  }
}