namespace TimeWarp.Architecture.Features.WeatherForecasts;

using MediatR;

public record GetWeatherForecastsRequest : BaseRequest, IApiRequest, IRequest<GetWeatherForecastsResponse>
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
