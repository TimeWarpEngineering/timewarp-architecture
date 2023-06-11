namespace TimeWarp.Architecture.Features.WeatherForecasts.Spa;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

[StateAccessMixin]
internal partial class WeatherForecastsState : State<WeatherForecastsState>
{
  private List<WeatherForecastDto> _WeatherForecasts { get; set; }

  public IReadOnlyList<WeatherForecastDto> WeatherForecasts => _WeatherForecasts.AsReadOnly();

  public WeatherForecastsState()
  {
    _WeatherForecasts = new List<WeatherForecastDto>();
  }

  public override void Initialize() { }
}
