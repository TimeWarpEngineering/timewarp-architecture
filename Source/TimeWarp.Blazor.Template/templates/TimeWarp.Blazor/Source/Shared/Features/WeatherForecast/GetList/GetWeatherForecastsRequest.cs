//#WeatherForecast #GetWeatherForecasts #Request #Api
namespace TimeWarp.Blazor.Features.WeatherForecasts
{
  using MediatR;
  using System;
  using TimeWarp.Blazor.Features.Bases;

  public class GetWeatherForecastsRequest : BaseRequest, IApiRequest, IRequest<GetWeatherForecastsResponse>
  {
    public const string Route = "api/weatherForecasts";

    /// <summary>
    /// The Number of days of forecasts to get
    /// </summary>
    /// <example>5</example>
    public int Days { get; set; }
    public HttpVerb GetHttpVerb() => HttpVerb.Get;
    public string GetRoute() => $"{Route}?{nameof(Days)}={Days}&{nameof(CorrelationId)}={CorrelationId}";

  }
}
