namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static GetWeatherForecasts;

[StateAccessMixin]
public sealed partial class WeatherForecastsState : State<WeatherForecastsState>
{
  private List<WeatherForecastDto>? WeatherForecastList { get; set; } = [];

  public IReadOnlyList<WeatherForecastDto>? WeatherForecasts => WeatherForecastList?.AsReadOnly();

  public override void Initialize() { WeatherForecastList = null; }
}
