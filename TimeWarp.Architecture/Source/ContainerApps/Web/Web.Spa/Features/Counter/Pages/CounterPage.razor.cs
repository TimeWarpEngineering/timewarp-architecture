namespace TimeWarp.Architecture.Pages;

using static ApplicationState;

[UsedImplicitly]
[Page("/Counter")]
public partial class CounterPage : BaseComponent
{
  private async Task ButtonClick() =>
    await Send(new RouteState.ChangeRoute.Action { NewRoute = "/" }).ConfigureAwait(false);

  private async Task ResetButtonClick() => await Send(new ResetStore.Action()).ConfigureAwait(false);
}
