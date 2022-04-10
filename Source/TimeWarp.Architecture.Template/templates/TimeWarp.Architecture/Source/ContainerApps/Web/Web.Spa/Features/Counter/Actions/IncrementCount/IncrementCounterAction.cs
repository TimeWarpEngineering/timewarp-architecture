namespace TimeWarp.Architecture.Features.Counters;
internal partial class CounterState
{
  public record IncrementCounterAction : BaseAction
  {
    public int Amount { get; set; }
  }
}
