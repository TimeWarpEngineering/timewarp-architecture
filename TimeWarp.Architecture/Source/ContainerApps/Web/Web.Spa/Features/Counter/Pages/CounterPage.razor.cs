namespace TimeWarp.Architecture.Pages;

[Page("/Counter")]
partial class CounterPage
{
  private async Task ButtonClick() =>
    await NoSubRouteState.ChangeRoute(newRoute: HomePage.GetPageUrl(), CancellationToken.None);

  private async Task ResetButtonClick() => await ApplicationState.ResetStore();
}
