namespace TimeWarp.Blazor.Client.Features.Counter.Components
{
  using TimeWarp.Blazor.Client.Features.Base.Components;
  using TimeWarp.Blazor.Client.Features.Counter;
  using System.Threading.Tasks;

  public class CounterBase : BaseComponent
  {
    protected async Task ButtonClick() =>
      _ = await Mediator.Send(new IncrementCounterAction { Amount = 5 });
  }
}