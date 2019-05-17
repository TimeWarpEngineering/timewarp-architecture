namespace BlazorHosted_CSharp.Client.Features.Application.Components
{
  using BlazorHosted_CSharp.Client.Features.Base.Components;

  public class SideBarModel: BaseComponent
  {
    protected string NavMenuCssClass => ApplicationState.IsMenuExpanded ? null : "collapse";
  }
}
  