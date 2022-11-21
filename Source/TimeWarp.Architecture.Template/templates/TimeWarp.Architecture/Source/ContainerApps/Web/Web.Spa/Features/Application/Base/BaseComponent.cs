namespace TimeWarp.Architecture.Features;

public partial class BaseComponent : BlazorStateDevToolsComponent, IAttributeComponent
{
  internal ApplicationState ApplicationState => GetState<ApplicationState>();  
}
