namespace TimeWarp.Blazor.Features.Applications.Components
{
  public partial class SideBar
  {
    protected string NavMenuCssClass => ApplicationState.IsMenuExpanded ? null : "collapse";
  }
}
