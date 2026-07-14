#region Purpose
// Code-behind for the counter demo page; wires handlers for route navigation home and a full store reset.
#endregion

namespace TimeWarp.Architecture.Pages;

[Page("/Counter")]
[CrossFeatureReference("The counter demo's reset button exercises the app-level store reset (ApplicationState) on purpose.")]
partial class CounterPage
{
  private async Task ButtonClick() =>
    await NoSubRouteState.ChangeRoute(newRoute: HomePage.GetPageUrl(), CancellationToken.None);

  private async Task ResetButtonClick() => await ApplicationState.ResetStore();
}
