namespace TimeWarp.Blazor.Features.Counters.Client
{
  using System.Threading.Tasks;
  using static TimeWarp.Blazor.Features.Counters.Client.CounterState;

  public partial class Counter
  {
    protected async Task ButtonClick() =>
      _ = await Mediator.Send(new IncrementCounterAction { Amount = 5 });
  }
}
