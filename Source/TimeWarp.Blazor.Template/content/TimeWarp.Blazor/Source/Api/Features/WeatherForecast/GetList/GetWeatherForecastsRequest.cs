namespace TimeWarp.Blazor.Api.Features.WeatherForecast
{
  using MediatR;
  using TimeWarp.Blazor.Api.Features.Base;

  public class GetWeatherForecastsRequest : BaseRequest, IRequest<GetWeatherForecastsResponse>
  {
    public const string Route = "api/weatherForecast";

    /// <summary>
    /// The Number of days of forecasts to get
    /// </summary>
    public int Days { get; set; }
  }
}
