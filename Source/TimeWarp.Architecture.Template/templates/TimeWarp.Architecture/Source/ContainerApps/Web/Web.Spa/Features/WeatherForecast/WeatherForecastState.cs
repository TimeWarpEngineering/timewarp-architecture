namespace TimeWarp.Architecture.Features.WeatherForecasts.Spa;

[StateAccessMixin]
internal partial class WeatherForecastsState : State<WeatherForecastsState>
{
  private List<Contracts.WeatherForecastDto> _WeatherForecasts { get; set; }

  public IReadOnlyList<Contracts.WeatherForecastDto> WeatherForecasts => _WeatherForecasts.AsReadOnly();

  public WeatherForecastsState()
  {
    _WeatherForecasts = new List<Contracts.WeatherForecastDto>();
  }

  public override void Initialize() { }
}
