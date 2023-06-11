namespace TimeWarp.Architecture.Features.WeatherForecasts;

public static partial class GetWeatherForecasts
{

  public sealed record Query : BaseRequest, IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public const string Route = "api/weatherForecasts";

    /// <summary>
    /// The Number of days of forecasts to get
    /// </summary>
    /// <example>5</example>
    public int Days { get; set; }
    public HttpVerb GetHttpVerb() => HttpVerb.Get;
    public string GetRoute() => $"{Route}?{nameof(Days)}={Days}";
  }

  public sealed record Response(IEnumerable<WeatherForecastDto> WeatherForecasts) : BaseResponse;


  /// <summary>
  /// The weather forecast
  /// </summary>
  public sealed class WeatherForecastDto
  {
    /// <summary>
    /// The forecast for this Date 
    /// </summary>
    /// <example>2020-06-08T12:32:39.9828696+07:00</example>
    public DateTime Date { get; set; }

    /// <summary>
    /// Summary of the forecast
    /// </summary>
    /// <example>Cool</example>
    public string Summary { get; set; }

    /// <summary>
    /// Temperature in Celsius
    /// </summary>
    /// <example>24</example>
    public int TemperatureC { get; set; }


    /// <summary>
    /// Temperature in Fahrenheit
    /// </summary>
    /// <example>75</example>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public WeatherForecastDto(DateTime date, string summary, int temperatureC)
    {
      Date = date;
      Summary = summary;
      TemperatureC = temperatureC;
    }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(query => query.Days).NotEmpty().GreaterThan(0);
    }
  }
}
