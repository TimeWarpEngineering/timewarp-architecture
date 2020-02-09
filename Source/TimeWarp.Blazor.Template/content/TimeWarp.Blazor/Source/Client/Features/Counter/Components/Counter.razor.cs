namespace TimeWarp.Blazor.CounterFeature
{
  using System.Threading.Tasks;
  using static TimeWarp.Blazor.CounterFeature.CounterState;

  public partial class Counter
  {
    protected async Task ButtonClick() =>
      _ = await Mediator.Send(new IncrementCounterAction { Amount = 5 });
  }
}
