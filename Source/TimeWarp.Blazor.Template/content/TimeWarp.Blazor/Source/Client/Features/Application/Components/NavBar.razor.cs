namespace TimeWarp.Blazor.ApplicationFeature
{
  using static TimeWarp.Blazor.ApplicationFeature.ApplicationState;

  public partial class NavBar
  {
    protected string NavMenuCssClass => ApplicationState.IsMenuExpanded ? null : "collapse";

    protected async void ToggleNavMenu() => await Mediator.Send(new ToggleMenuAction());
  }
}
