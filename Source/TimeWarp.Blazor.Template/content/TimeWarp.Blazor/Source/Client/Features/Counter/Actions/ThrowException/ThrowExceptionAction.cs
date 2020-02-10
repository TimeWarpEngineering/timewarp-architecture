namespace TimeWarp.Blazor.Features.Counters
{
  using TimeWarp.Blazor.Features.Bases;

  internal partial class CounterState
  {
    public class ThrowExceptionAction : BaseAction
    {
      public string Message { get; set; }
    }
  }
}
