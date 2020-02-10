namespace TimeWarp.Blazor.Features.Applications.Client.Components
{
  public partial class SideBar
  {
    protected string NavMenuCssClass => ApplicationState.IsMenuExpanded ? null : "collapse";
  }
}
