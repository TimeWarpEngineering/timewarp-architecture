namespace TimeWarp.Blazor.Client.ApplicationFeature
{
  using TimeWarp.Blazor.Client.Features.Base.Components;
  using static TimeWarp.Blazor.Client.ApplicationFeature.ApplicationState;

  public partial class NavBar
  {
    protected string NavMenuCssClass => ApplicationState.IsMenuExpanded ? null : "collapse";

    protected async void ToggleNavMenu() => await Mediator.Send(new ToggleMenuAction());
  }
}