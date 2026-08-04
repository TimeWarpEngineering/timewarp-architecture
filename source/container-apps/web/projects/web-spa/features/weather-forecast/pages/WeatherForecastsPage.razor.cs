#region Purpose
// Code-behind for the weather forecasts page: declares the route; the fetch-on-init logic lives in the .razor @code block.
#endregion

namespace TimeWarp.Architecture.Features.WeatherForecasts;

[Page("/WeatherForecasts", Policy = Policies.CanViewDeveloperPage)]
[Authorize(Policy = Policies.CanViewDeveloperPage)]
partial class WeatherForecastsPage;
