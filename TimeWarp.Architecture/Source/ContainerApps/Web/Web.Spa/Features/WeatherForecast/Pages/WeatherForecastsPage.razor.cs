namespace TimeWarp.Architecture.Pages;

using static WeatherForecastsState;

[Page("/WeatherForecasts")]
partial class WeatherForecastsPage
{
  [Parameter] [SupplyParameterFromQuery] public int? Days { get; set; }

  protected override async Task OnInitializedAsync() =>
    await Send(new FetchWeatherForecasts.Action(Days));
}
