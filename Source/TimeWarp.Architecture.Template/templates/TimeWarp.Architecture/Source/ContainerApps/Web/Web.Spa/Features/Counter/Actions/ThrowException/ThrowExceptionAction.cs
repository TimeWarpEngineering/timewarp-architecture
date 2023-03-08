namespace TimeWarp.Architecture.Features.Counters;
internal partial class CounterState
{
  public record ThrowExceptionAction(string Message) : BaseAction;
}
