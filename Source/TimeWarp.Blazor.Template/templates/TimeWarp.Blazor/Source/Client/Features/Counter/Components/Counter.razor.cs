namespace TimeWarp.Blazor.Features.Counters.Components
{
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Components;
  using TimeWarp.Blazor.Features.Bases;
  using static TimeWarp.Blazor.Features.Counters.CounterState;

  public partial class Counter : BaseComponent, IAttributeComponent
  {
    protected async Task ButtonClick() => await Send(new IncrementCounterAction { Amount = 5 });
  }
}
