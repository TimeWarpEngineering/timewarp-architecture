using TimeWarp.Blazor.Features.Bases.Client;

namespace TimeWarp.Blazor.Features.Applications.Components
{
  public partial class SideBar: BaseComponent
  {
    protected string NavMenuCssClass => ApplicationState.IsMenuExpanded ? null : "collapse";
  }
}
