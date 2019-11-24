namespace TimeWarp.Blazor.Client.ApplicationFeature
{
  using TimeWarp.Blazor.Client.Features.Base.Components;

  public partial class SideBar
  {
    protected string NavMenuCssClass => ApplicationState.IsMenuExpanded ? null : "collapse";
  }
}
  