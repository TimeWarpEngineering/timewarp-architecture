namespace TimeWarp.Architecture.Features.Counters.Components;

using static TimeWarp.Architecture.Features.Counters.CounterState;

public partial class Counter : BaseComponent, IAttributeComponent
{
  protected async Task ButtonClick() => await Send(new IncrementCounter.Action(Amount: 5));
}
