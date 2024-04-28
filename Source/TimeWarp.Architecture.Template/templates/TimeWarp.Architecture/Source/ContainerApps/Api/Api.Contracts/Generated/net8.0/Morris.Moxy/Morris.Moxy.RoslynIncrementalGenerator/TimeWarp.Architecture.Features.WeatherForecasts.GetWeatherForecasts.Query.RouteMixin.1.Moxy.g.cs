// Generated at 2024-04-28 18:25:48 UTC

namespace TimeWarp.Architecture.Features.WeatherForecasts;

public partial class GetWeatherForecasts
{
  public partial class Query
  {
    public const string RouteTemplate = "api/weatherForecasts";
    public HttpVerb GetHttpVerb() => HttpVerb.Get;
    public string GetRoute() => FormattableString.Invariant($"api/weatherForecasts");
  }
}
