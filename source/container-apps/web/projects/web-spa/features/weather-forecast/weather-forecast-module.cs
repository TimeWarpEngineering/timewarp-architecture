#region Purpose
// DI registration hook for the weather forecast feature; the body is empty because the feature needs nothing beyond shared services.
#endregion

namespace TimeWarp.Architecture.Features.WeatherForecasts;

public class WeatherForecastModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration) { }
}
