namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

public class GetWeatherForecastsHandler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
{
  private readonly string[] Summaries = new[]
  {
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
  };

  public Task<OneOf<Response, SharedProblemDetails>> Handle
  (
    Query query,
    CancellationToken aCancellationToken
  )
  {
    var random = new Random();

    List<WeatherForecastDto> weatherForecastDtos = new();

    Enumerable.Range(1, query.Days).ToList().ForEach
    (
      aIndex => weatherForecastDtos.Add
      (
        new WeatherForecastDto
        (
          date: DateTime.Now.AddDays(aIndex),
          summary: Summaries[random.Next(Summaries.Length)],
          temperatureC: random.Next(-20, 55)
        )
      )
    );
    var response = new Response(weatherForecastDtos);

    return Task.FromResult((OneOf<Response, SharedProblemDetails>)response);
  }
}
