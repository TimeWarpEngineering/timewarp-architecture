namespace TimeWarp.Architecture.Features;

public partial class BaseComponent : BlazorStateDevToolsComponent, IAttributeComponent
{
  internal EventStreamState EventStreamState => GetState<EventStreamState>();
}
