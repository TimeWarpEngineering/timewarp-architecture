namespace TimeWarp.Architecture.Features.WeatherForecasts;

/// <summary>
/// Get Weather Forecasts
/// </summary>
/// <remarks>
/// Gets Weather Forecasts for the number of days specified in the request
/// </remarks>
public class WeatherEndpoint : Endpoint<WeatherRequest, IEnumerable<WeatherResponse>>
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
    WeatherRequest aRequest,
    CancellationToken aCancellationToken
  )
  {
    int days = aRequest.Days ?? 5;

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
