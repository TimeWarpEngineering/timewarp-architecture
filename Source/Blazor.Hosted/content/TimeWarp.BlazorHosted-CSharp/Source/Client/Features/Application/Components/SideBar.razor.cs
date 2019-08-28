namespace TimeWarp.Blazor.Client.Features.Application.Components
{
  using TimeWarp.Blazor.Client.Features.Base.Components;

  public class SideBarBase: BaseComponent
  {
    protected string NavMenuCssClass => ApplicationState.IsMenuExpanded ? null : "collapse";
  }
}
  