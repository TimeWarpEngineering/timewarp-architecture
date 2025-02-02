namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

/// <summary>
/// Get Weather Forecasts
/// </summary>
/// <remarks>
/// Gets Weather Forecasts for the number of days specified in the request
/// </remarks>
public class GetWeatherForecastsEndpoint : Endpoint<Query, IEnumerable<Response>>
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
    Get(GetWeatherForecasts.Query.RouteTemplate);
    AllowAnonymous();
    Description
    (
      d => d
        .Produces<IEnumerable<Response>>(200)
        .ProducesProblem(400)
        .WithTags("Weather")
    );
  }

  public override async Task HandleAsync
  (
    Query aQuery,
    CancellationToken aCancellationToken
  )
  {
    int days = aQuery.Days ?? 5;

    IEnumerable<Response> forecasts = Enumerable.Range(1, days).Select
    (
      index => new Response
      (
        DateTime.Now.AddDays(index),
        Summaries[Random.Shared.Next(Summaries.Length)],
        Random.Shared.Next(-20, 55)
      )
    );

    await SendAsync(forecasts, cancellation: aCancellationToken);
  }
}
