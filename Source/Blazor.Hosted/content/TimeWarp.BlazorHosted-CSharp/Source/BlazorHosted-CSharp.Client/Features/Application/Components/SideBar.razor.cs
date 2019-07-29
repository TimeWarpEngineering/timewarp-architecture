namespace BlazorHosted_CSharp.Client.Features.Application.Components
{
  using BlazorHosted_CSharp.Client.Features.Base.Components;

  public class SideBarBase: BaseComponent
  {
    protected string NavMenuCssClass => ApplicationState.IsMenuExpanded ? null : "collapse";
  }
}
  