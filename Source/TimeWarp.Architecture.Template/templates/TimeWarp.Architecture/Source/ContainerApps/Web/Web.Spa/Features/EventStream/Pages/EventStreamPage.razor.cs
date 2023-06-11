namespace TimeWarp.Architecture.Pages;

using static TimeWarp.Architecture.Features.Applications.ApplicationState;

[Page("/EventStream")]
public partial class EventStreamPage : BaseComponent
{
  private async Task ButtonClick() =>
    await Send(new RouteState.ChangeRouteAction { NewRoute = "/" });

  private async Task ResetButtonClick() => await Send(new ResetStore.Action());
}
