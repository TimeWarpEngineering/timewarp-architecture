namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static GetWeatherForecasts;

[StateAccessMixin]
public sealed partial class WeatherForecastsState : State<WeatherForecastsState>
{
  private List<TWeatherForecast>? WeatherForecastList { get; set; } = [];

  public IReadOnlyList<TWeatherForecast>? WeatherForecasts => WeatherForecastList?.AsReadOnly();

  public override void Initialize() { WeatherForecastList = null; }
}
