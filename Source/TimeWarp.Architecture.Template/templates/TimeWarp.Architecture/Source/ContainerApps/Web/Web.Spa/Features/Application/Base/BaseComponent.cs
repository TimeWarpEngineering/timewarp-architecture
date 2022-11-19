namespace TimeWarp.Architecture.Features.Base;

public partial class BaseComponent : BlazorStateDevToolsComponent, IAttributeComponent
{
  internal ApplicationState ApplicationState => GetState<ApplicationState>();  
}
