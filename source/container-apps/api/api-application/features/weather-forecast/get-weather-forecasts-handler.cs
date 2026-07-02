#region Purpose
// Serves the GetWeatherForecasts sample query with synthesized data.
#endregion

#region Design
// Template demonstration of the application side of a vertical slice: no persistence — random
// in-memory forecasts keep the sample runnable with zero infrastructure.
// Only the OneOf success arm is returned; the SharedProblemDetails arm exists to model the
// error-return pattern real handlers should follow.
#endregion

namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static GetWeatherForecasts;

public class GetWeatherForecastsHandler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
{
  private readonly string[] Summaries =
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

  public Task<OneOf<Response, SharedProblemDetails>> Handle
  (
    Query query,
    CancellationToken cancellationToken
  )
  {
    var random = new Random();

    List<TWeatherForecast> weatherForecasts = [];

    Enumerable.Range(1, query?.Days ?? 5).ToList().ForEach
    (
      index => weatherForecasts.Add
      (
        new TWeatherForecast
        (
          date: DateTime.Now.AddDays(index),
          summary: Summaries[random.Next(Summaries.Length)],
          temperatureC: random.Next(-20, 55)
        )
      )
    );
    var response = new Response(weatherForecasts);

    return Task.FromResult((OneOf<Response, SharedProblemDetails>)response);
  }
}
