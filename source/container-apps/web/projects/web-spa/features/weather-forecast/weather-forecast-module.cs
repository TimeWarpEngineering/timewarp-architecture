#region Purpose
// DI registration hook for the weather forecast feature; the body is empty because the feature needs nothing beyond shared services.
#endregion

namespace TimeWarp.Architecture.Features.WeatherForecasts;

public class WeatherForecastModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    _ = serviceCollection; // Avoids "unused parameter" warning; the method is a placeholder for documentation and future use.
    _ = configuration; // Avoids "unused parameter" warning; the method is a placeholder for documentation and future use.
  }
}
