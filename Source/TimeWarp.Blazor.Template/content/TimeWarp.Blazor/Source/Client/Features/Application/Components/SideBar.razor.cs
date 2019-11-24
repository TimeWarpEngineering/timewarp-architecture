namespace TimeWarp.Blazor.Client.ApplicationFeature
{
  public partial class SideBar
  {
    protected string NavMenuCssClass => ApplicationState.IsMenuExpanded ? null : "collapse";
  }
}