namespace TimeWarp.Architecture.Pages;

using static WeatherForecastsState;

[Page("/WeatherForecasts")]
public partial class WeatherForecastsPage : BaseComponent
{
  [Parameter] [SupplyParameterFromQuery] public int? Days { get; set; }

  protected override async Task OnInitializedAsync() =>
    await Send(new FetchWeatherForecasts.Action(Days));
}
