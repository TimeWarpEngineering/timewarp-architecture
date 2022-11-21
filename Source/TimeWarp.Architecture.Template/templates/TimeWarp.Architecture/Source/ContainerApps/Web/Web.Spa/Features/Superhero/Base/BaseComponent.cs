namespace TimeWarp.Architecture.Features;

public partial class BaseComponent : BlazorStateDevToolsComponent, IAttributeComponent
{
  internal SuperheroState SuperheroState => GetState<SuperheroState>();
}
