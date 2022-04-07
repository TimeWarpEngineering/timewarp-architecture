namespace TimeWarp.Architecture.Features.Counters;

using TimeWarp.Architecture.Features.Bases;

internal partial class CounterState
{
  public class ThrowExceptionAction : BaseAction
  {
    public string Message { get; set; }
  }
}
