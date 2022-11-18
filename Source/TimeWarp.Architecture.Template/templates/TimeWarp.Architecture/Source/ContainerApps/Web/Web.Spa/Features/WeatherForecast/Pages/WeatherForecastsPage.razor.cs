namespace TimeWarp.Architecture.Pages;

using TimeWarp.Architecture.Features.Base;
using static TimeWarp.Architecture.Features.WeatherForecasts.WeatherForecastsState;

public partial class WeatherForecastsPage : BaseComponent
{
  private const string RouteTemplate = "/WeatherForecasts";

  public static string GetRoute() => RouteTemplate;

  protected override async Task OnInitializedAsync() =>
    await Send(new FetchWeatherForecastsAction()).ConfigureAwait(false);
}
