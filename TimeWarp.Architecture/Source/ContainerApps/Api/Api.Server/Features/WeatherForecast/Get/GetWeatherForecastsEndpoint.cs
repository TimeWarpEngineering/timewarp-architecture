namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

/// <summary>
/// Get Weather Forecasts
/// </summary>
/// <remarks>
/// Gets Weather Forecasts for the number of days specified in the request
/// </remarks>
public class GetWeatherForecastsEndpoint : BaseFastEndpoint<Query, Response>
{
  public override void Configure()
  {
    Get(GetWeatherForecasts.Query.RouteTemplate);
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "Get Weather Forecasts";
      s.Description = "Gets Weather Forecasts for the number of days specified in the request";
    });
    Description(d => d
      .Produces<IEnumerable<Response>>(200)
      .ProducesProblem(400)
    );
    Tags("Weather");
  }
}
