namespace TimeWarp.Architecture.Pages;

using static TimeWarp.Architecture.Features.WeatherForecasts.Spa.WeatherForecastsState;

[Page("/WeatherForecasts")]
public partial class WeatherForecastsPage : BaseComponent
{
  protected override async Task OnInitializedAsync() =>
    await Send(new FetchWeatherForecastsAction()).ConfigureAwait(false);
}
