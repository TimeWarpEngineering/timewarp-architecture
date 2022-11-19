namespace TimeWarp.Architecture.Features.Base;

public partial class BaseComponent : BlazorStateDevToolsComponent, IAttributeComponent
{
  internal WeatherForecastsState WeatherForecastsState => GetState<WeatherForecastsState>();
}
