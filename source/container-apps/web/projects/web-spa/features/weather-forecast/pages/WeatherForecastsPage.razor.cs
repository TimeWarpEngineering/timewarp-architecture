#region Purpose
// Code-behind for the weather forecasts page: route, fetch-on-init, TrackAction loading.
#endregion

#region Design
// Loading is FetchWeatherForecasts [TrackAction] (COPIC IsAnyActive), not WeatherForecasts is null.
#endregion

namespace TimeWarp.Architecture.Features.WeatherForecasts;

[Page("/WeatherForecasts", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class WeatherForecastsPage
{
  [Parameter]
  [SupplyParameterFromQuery]
  public int? Days { get; set; }

  private bool IsLoading =>
    IsAnyActive(typeof(WeatherForecastsState.FetchWeatherForecastsActionSet.Action));

  protected override async Task OnInitializedAsync() =>
    await WeatherForecastsState.FetchWeatherForecasts(Days);
}
