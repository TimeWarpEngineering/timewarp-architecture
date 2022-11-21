namespace TimeWarp.Architecture.Features;

public partial class BaseComponent : BlazorStateDevToolsComponent, IAttributeComponent
{
  internal CounterState CounterState => GetState<CounterState>();
}
