namespace TimeWarp.Architecture.Features.WeatherForecasts;

using static GetWeatherForecasts;

[UsedImplicitly]
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

    List<WeatherForecastDto> weatherForecasts = [];

    Enumerable.Range(1, query?.Days ?? 5).ToList().ForEach
    (
      index => weatherForecasts.Add
      (
        new WeatherForecastDto
        {
          Date = DateTime.Now.AddDays(index),
          Summary = Summaries[random.Next(Summaries.Length)],
          TemperatureC = random.Next(-20, 55)
        }
      )
    );
    var response = new Response(weatherForecasts);

    return Task.FromResult((OneOf<Response, SharedProblemDetails>)response);
  }
}
