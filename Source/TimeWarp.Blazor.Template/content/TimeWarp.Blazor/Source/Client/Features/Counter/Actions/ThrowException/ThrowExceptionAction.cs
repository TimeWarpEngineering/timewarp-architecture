namespace TimeWarp.Blazor.Features.Counters.Client
{
  using TimeWarp.Blazor.Features.Bases.Client;

  internal partial class CounterState
  {
    public class ThrowExceptionAction : BaseAction
    {
      public string Message { get; set; }
    }
  }
}
