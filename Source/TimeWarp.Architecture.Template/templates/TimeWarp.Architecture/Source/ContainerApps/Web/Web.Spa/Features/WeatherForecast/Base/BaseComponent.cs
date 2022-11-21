namespace TimeWarp.Architecture.Features;

public partial class BaseComponent : BlazorStateDevToolsComponent
{
  internal WeatherForecastsState WeatherForecastsState => GetState<WeatherForecastsState>();
}
