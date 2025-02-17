namespace TimeWarp.Architecture.Features.WeatherForecasts;

[ApiEndpoint]
public static partial class GetWeatherForecasts
{
  [RouteMixin("api/weatherforecast", HttpVerb.Get)]
  public sealed partial class Query : IRequest<OneOf<Response, SharedProblemDetails>>, IQueryStringRouteProvider,
    IApiRequest
  {
    /// <summary>
    /// The Number of days of forecasts to get
    /// </summary>
    /// <example>5</example>
    public int? Days { get; set; }

    public string GetRouteWithQueryString()
    {
      var parameters = new NameValueCollection { { nameof(Days), Days?.ToString() } };

      return $"{GetRoute()}?{this.GetQueryString(parameters)}";
    }
  }

  public sealed class Response(IEnumerable<WeatherForecastDto> WeatherForecasts) : BaseResponse
  {
    public IEnumerable<WeatherForecastDto> WeatherForecasts { get; init; } = WeatherForecasts;
  }

  /// <summary>
  /// The weather forecast
  /// </summary>
  public sealed class WeatherForecastDto
  {
    /// <summary>
    /// The forecast for this Date
    /// </summary>
    /// <example>2020-06-08T12:32:39.9828696+07:00</example>
    public DateTime Date { get; }

    /// <summary>
    /// Summary of the forecast
    /// </summary>
    /// <example>Cool</example>
    public string Summary { get; }

    /// <summary>
    /// Temperature in Celsius
    /// </summary>
    /// <example>24</example>
    public int TemperatureC { get; }

    /// <summary>
    /// Temperature in Fahrenheit
    /// </summary>
    /// <example>75</example>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public WeatherForecastDto(DateTime date, string summary, int temperatureC)
    {
      Date = Guard.Against.NullOrOutOfSQLDateRange(date);
      Summary = Guard.Against.NullOrWhiteSpace(summary);
      TemperatureC = temperatureC;
    }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(x => x.Days)
        .GreaterThanOrEqualTo(1)
        .WithMessage("Days must be greater than or equal to 1");
    }
  }
}
