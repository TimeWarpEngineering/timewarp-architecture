namespace TimeWarp.Architecture.Features.WeatherForecasts;

[TwBaseSpa]
internal partial class WeatherForecastsState : State<WeatherForecastsState>
{
  private List<WeatherForecastDto> _WeatherForecasts;

  public IReadOnlyList<WeatherForecastDto> WeatherForecasts => _WeatherForecasts.AsReadOnly();

  public WeatherForecastsState()
  {
    _WeatherForecasts = new List<WeatherForecastDto>();
  }

  public override void Initialize() { }
}
