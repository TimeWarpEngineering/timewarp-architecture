namespace TimeWarp.Blazor.Client.Features.Counter.Components
{
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Client.Features.Base.Components;
  using static TimeWarp.Blazor.Client.Features.Counter.CounterState;

  public class CounterBase : BaseComponent
  {
    protected async Task ButtonClick() =>
      _ = await Mediator.Send(new IncrementCounterAction { Amount = 5 });
  }
}