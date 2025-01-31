namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static TimeWarp.Architecture.Features.WeatherForecasts.Weather;

/// <summary>
/// Get Weather Forecasts
/// </summary>
/// <remarks>
/// Gets Weather Forecasts for the number of days specified in the request
/// </remarks>
public class WeatherEndpoint : Endpoint<Request, IEnumerable<Response>>
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
        .Produces<IEnumerable<Response>>(200)
        .ProducesProblem(400)
        .WithTags("Weather")
    );
  }

  public override async Task HandleAsync
  (
    Request aRequest,
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
