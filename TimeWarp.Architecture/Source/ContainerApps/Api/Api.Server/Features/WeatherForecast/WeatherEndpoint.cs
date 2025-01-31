namespace TimeWarp.Architecture.Features.WeatherForecasts;

/// <summary>
/// Get Weather Forecasts
/// </summary>
/// <remarks>
/// Gets Weather Forecasts for the number of days specified in the request
/// </remarks>
public class WeatherEndpoint : EndpointWithoutRequest<IEnumerable<WeatherResponse>>
{
  private static readonly string[] Summaries =
  [
    "Freezing",
    "Bracing",
    "Chilly",
    "Cool",
    "Mild",
    "Warm",
    "Balmy",
    "Hot",
    "Sweltering",
    "Scorching"
  ];

  public override void Configure()
  {
    Get("api/weather");
    AllowAnonymous();
    Description
    (
      d => d
        .Produces<IEnumerable<WeatherResponse>>(200)
        .ProducesProblem(400)
        .WithTags("Weather")
    );
  }

  public override async Task HandleAsync
  (
    CancellationToken aCancellationToken
  )
  {
    int days = Query<int?>("days") ?? 5;

    if (days <= 0)
    {
      AddError("Days must be greater than 0");
      await SendErrorsAsync(400, cancellation: aCancellationToken);
      return;
    }

    IEnumerable<WeatherResponse> forecasts = Enumerable.Range(1, days).Select
    (
      index => new WeatherResponse
      (
        DateTime.Now.AddDays(index),
        Summaries[Random.Shared.Next(Summaries.Length)],
        Random.Shared.Next(-20, 55)
      )
    );

    await SendAsync(forecasts, cancellation: aCancellationToken);
  }
}

/// <summary>
/// Weather forecast response
/// </summary>
public sealed class WeatherResponse
{
  /// <summary>
  /// The forecast for this Date
  /// </summary>
  /// <example>2020-06-08T12:32:39.9828696+07:00</example>
  public DateTime Date { get; init; }

  /// <summary>
  /// Summary of the forecast
  /// </summary>
  /// <example>Cool</example>
  public string Summary { get; init; }

  /// <summary>
  /// Temperature in Celsius
  /// </summary>
  /// <example>24</example>
  public int TemperatureC { get; init; }

  /// <summary>
  /// Temperature in Fahrenheit
  /// </summary>
  /// <example>75</example>
  public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

  public WeatherResponse
  (
    DateTime date,
    string summary,
    int temperatureC
  )
  {
    Date = date;
    Summary = summary;
    TemperatureC = temperatureC;
  }
}
