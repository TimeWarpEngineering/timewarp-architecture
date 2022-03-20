namespace TimeWarp.Architecture.Features.Counters
{
  using TimeWarp.Architecture.Features.Bases;

  internal partial class CounterState
  {
    public class IncrementCounterAction : BaseAction
    {
      public int Amount { get; set; }
    }
  }
}
