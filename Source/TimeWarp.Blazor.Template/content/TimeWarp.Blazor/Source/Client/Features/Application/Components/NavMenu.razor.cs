namespace TimeWarp.Blazor.Features.Applications.Client.Components
{
  using Microsoft.AspNetCore.Components;

  public class NavMenuBase : ComponentBase
  {
    protected bool CollapseNavMenu { get; set; }

    protected string NavMenuCssClass => CollapseNavMenu ? "collapse" : null;

    protected void ToggleNavMenu() => CollapseNavMenu = !CollapseNavMenu;
  }
}
