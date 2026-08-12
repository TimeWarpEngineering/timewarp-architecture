#region Purpose
// Code-behind for the counter demo page; wires handlers for route navigation home and a full store reset.
#endregion

namespace TimeWarp.Architecture.Features.Counters;

[Page("/Counter", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class CounterPage
{
  private async Task ButtonClick() =>
    await NoSubRouteState.ChangeRoute(newRoute: HomePage.GetPageUrl(), CancellationToken.None);

  private async Task ResetButtonClick() => await ApplicationState.ResetStore();
}
