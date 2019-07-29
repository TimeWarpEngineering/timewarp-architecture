namespace BlazorHosted_CSharp.Client.Features.Application.Components
{
  using BlazorHosted_CSharp.Client.Features.Base.Components;

  public class NavBarBase : BaseComponent
  {
    protected string NavMenuCssClass => ApplicationState.IsMenuExpanded ? null : "collapse";
    protected async void ToggleNavMenu() => await Mediator.Send(new ToggleMenuAction());
  }
}
