namespace TimeWarp.Blazor.Client.CounterFeature
{
  using System.Threading.Tasks;
  using static TimeWarp.Blazor.Client.Features.Counter.CounterState;

  public partial class Counter
  {
    protected async Task ButtonClick() =>
      _ = await Mediator.Send(new IncrementCounterAction { Amount = 5 });
  }
}