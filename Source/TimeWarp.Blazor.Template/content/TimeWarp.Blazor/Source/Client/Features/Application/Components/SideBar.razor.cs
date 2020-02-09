namespace TimeWarp.Blazor.ApplicationFeature
{
  public partial class SideBar
  {
    protected string NavMenuCssClass => ApplicationState.IsMenuExpanded ? null : "collapse";
  }
}
