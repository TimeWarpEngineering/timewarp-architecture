#region Purpose
// Endpoint-centric contract for the sample weather-forecast query.
#endregion

#region Design
// The template's reference contract: nested Query/Response/Validator with [ApiRoute] feeding the
// FastEndpoint source generator, so no hand-written endpoint class exists.
// IQueryStringRouteProvider is implemented because GET carries its parameters in the query
// string; clients build the URL from the contract instead of duplicating the route.
// XML docs and <example> tags flow into the generated OpenAPI description.
// TWeatherForecast guards its invariants in the constructor so an invalid forecast cannot be
// constructed.
// [EndpointAllowAnonymous] (task 110): public demo data, no security surface — the template's
// canonical "hello world" style reference contract is meant to be reachable with zero setup.
#endregion

namespace TimeWarp.Architecture.Features.WeatherForecasts;

[ApiEndpoint]
[EndpointAllowAnonymous("Public demo data with no security surface; the template's reference contract is meant to be reachable with zero setup.")]
public static partial class GetWeatherForecasts
{
  [ApiRoute("api/weatherforecast", HttpVerb.Get)]
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

  public sealed class Response(IEnumerable<TWeatherForecast> WeatherForecasts) : BaseResponse
  {
    public IEnumerable<TWeatherForecast> WeatherForecasts { get; init; } = WeatherForecasts;
  }

  /// <summary>
  /// The weather forecast
  /// </summary>
  public sealed class TWeatherForecast
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

    public TWeatherForecast(DateTime date, string summary, int temperatureC)
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
