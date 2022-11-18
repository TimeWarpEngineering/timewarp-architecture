namespace TimeWarp.Architecture.Features.Base;

public partial class BaseComponent : BlazorStateDevToolsComponent, IAttributeComponent
{
  internal SuperheroState SuperheroState => GetState<SuperheroState>();
}
