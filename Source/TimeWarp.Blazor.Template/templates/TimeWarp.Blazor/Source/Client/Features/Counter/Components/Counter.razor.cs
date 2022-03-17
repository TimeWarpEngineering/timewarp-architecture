namespace TimeWarp.Architecture.Features.Counters.Components
{
  using System.Threading.Tasks;
  using TimeWarp.Architecture.Components;
  using TimeWarp.Architecture.Features.Bases;
  using static TimeWarp.Architecture.Features.Counters.CounterState;

  public partial class Counter : BaseComponent, IAttributeComponent
  {
    protected async Task ButtonClick() => await Send(new IncrementCounterAction { Amount = 5 }).ConfigureAwait(false);
  }
}
