namespace TimeWarp.Blazor.Features.Counters.Client
{
  using TimeWarp.Blazor.Features.Bases.Client;

  internal partial class CounterState
  {
    public class IncrementCounterAction : BaseAction
    {
      public int Amount { get; set; }
    }
  }
}
