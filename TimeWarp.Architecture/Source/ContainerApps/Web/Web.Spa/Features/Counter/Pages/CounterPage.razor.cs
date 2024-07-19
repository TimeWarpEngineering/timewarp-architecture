namespace TimeWarp.Architecture.Pages;

using static ApplicationState;

[UsedImplicitly]
[Page("/Counter")]
public partial class CounterPage : BaseComponent
{
  private async Task ButtonClick() =>
    await NoSubRouteState.ChangeRoute(newRoute: "/", CancellationToken.None);

  private async Task ResetButtonClick() => await Send(new ResetStore.Action()).ConfigureAwait(false);
}
