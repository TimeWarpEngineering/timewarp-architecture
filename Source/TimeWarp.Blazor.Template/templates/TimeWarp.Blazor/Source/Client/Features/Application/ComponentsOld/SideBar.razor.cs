namespace TimeWarp.Blazor.Features.Applications.Components
{
  using TimeWarp.Blazor.Features.Bases;

  public partial class SideBar: BaseComponent
  {
    protected string NavMenuCssClass => ApplicationState.IsMenuExpanded ? null : "collapse";
  }
}
