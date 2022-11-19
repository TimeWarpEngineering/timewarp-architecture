namespace TimeWarp.Architecture.Features.Base;

public partial class BaseComponent : BlazorStateDevToolsComponent, IAttributeComponent
{
  internal CounterState CounterState => GetState<CounterState>();
}
