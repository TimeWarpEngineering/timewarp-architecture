namespace TimeWarp.Architecture.Features.Counters;
internal partial class CounterState
{
  public record ThrowExceptionAction : BaseAction
  {
    public string Message { get; set; }
  }
}
