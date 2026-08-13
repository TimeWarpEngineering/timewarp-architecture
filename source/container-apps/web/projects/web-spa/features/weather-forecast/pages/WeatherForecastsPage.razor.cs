#region Purpose
// Registers the WeatherForecasts route and authorize policy; markup and behavior live in WeatherForecastsPage.razor.
#endregion

#region Design
// Loading is FetchWeatherForecasts [TrackAction] (COPIC IsAnyActive), not WeatherForecasts is null.
#endregion

namespace TimeWarp.Architecture.Features.WeatherForecasts;

[Page("/WeatherForecasts", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class WeatherForecastsPage;
