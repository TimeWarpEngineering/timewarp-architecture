namespace TimeWarp.Architecture.Features.Counters;
internal partial class CounterState
{
  public record IncrementCounterAction(int Amount) : BaseAction;

}
