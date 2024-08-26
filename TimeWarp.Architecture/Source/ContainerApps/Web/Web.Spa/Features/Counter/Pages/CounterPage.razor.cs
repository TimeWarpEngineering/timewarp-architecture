namespace TimeWarp.Architecture.Pages;

using static ApplicationState;

[Page("/Counter")]
partial class CounterPage
{
  private async Task ButtonClick() =>
    await NoSubRouteState.ChangeRoute(newRoute: "/", CancellationToken.None);

  private async Task ResetButtonClick() => await Send(new ResetStore.Action()).ConfigureAwait(false);
}
