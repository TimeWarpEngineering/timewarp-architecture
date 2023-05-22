namespace TimeWarp.Architecture.Features.Counters.Components;

using static TimeWarp.Architecture.Features.Counters.Spa.CounterState;

public partial class Counter : BaseComponent, IAttributeComponent
{
  protected async Task ButtonClick() => await Send(new IncrementCounterAction(Amount: 5));
}
