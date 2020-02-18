namespace TimeWarp.Blazor.Features.Applications.Components
{
  using Microsoft.AspNetCore.Components;

  public partial class NavMenu : ComponentBase
  {
    protected bool CollapseNavMenu { get; set; }

    protected string NavMenuCssClass => CollapseNavMenu ? "collapse" : null;

    protected void ToggleNavMenu() => CollapseNavMenu = !CollapseNavMenu;
  }
}
