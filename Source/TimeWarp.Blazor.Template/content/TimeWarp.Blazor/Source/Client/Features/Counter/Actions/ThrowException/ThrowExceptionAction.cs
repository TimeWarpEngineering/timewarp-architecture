namespace TimeWarp.Blazor.Client.Features.Counter
{
  using TimeWarp.Blazor.Client.Features.Base;

  internal partial class CounterState
  {
    public class ThrowExceptionAction : BaseAction
    {
      public string Message { get; set; }
    }
  }
}