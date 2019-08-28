namespace TimeWarp.Blazor.Client.Features.Application.Components
{
  using TimeWarp.Blazor.Client.Features.Base.Components;

  public class NavBarBase : BaseComponent
  {
    protected string NavMenuCssClass => ApplicationState.IsMenuExpanded ? null : "collapse";
    protected async void ToggleNavMenu() => await Mediator.Send(new ToggleMenuAction());
  }
}
