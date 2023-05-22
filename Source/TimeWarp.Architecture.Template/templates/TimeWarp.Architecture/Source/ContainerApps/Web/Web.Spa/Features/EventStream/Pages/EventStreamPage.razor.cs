namespace TimeWarp.Architecture.Pages;

using static TimeWarp.Architecture.Features.Applications.Spa.ApplicationState;

[Page("/EventStream")]
public partial class EventStreamPage : BaseComponent
{
  private async Task ButtonClick() =>
    await Send(new RouteState.ChangeRouteAction { NewRoute = "/" }).ConfigureAwait(false);

  private async Task ResetButtonClick() => await Send(new ResetStoreAction()).ConfigureAwait(false);
}
