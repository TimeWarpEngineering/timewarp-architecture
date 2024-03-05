namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

[StateAccessMixin]
internal partial class WeatherForecastsState : State<WeatherForecastsState>
{
  private List<WeatherForecastDto> WeatherForecastList { get; set; } = [];

  public IReadOnlyList<WeatherForecastDto> WeatherForecasts => WeatherForecastList.AsReadOnly();

  public override void Initialize() { }
}
