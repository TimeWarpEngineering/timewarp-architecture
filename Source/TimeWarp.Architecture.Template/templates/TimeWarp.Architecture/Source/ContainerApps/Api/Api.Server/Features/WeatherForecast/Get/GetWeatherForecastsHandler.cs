namespace TimeWarp.Architecture.Features.WeatherForecasts;

using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class GetWeatherForecastsHandler : IRequestHandler<GetWeatherForecastsRequest, GetWeatherForecastsResponse>
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

  public Task<GetWeatherForecastsResponse> Handle
  (
    GetWeatherForecastsRequest aGetWeatherForecastsRequest,
    CancellationToken aCancellationToken
  )
  {
    var random = new Random();

    List<WeatherForecastDto> weatherForecastDtos = new();

    Enumerable.Range(1, aGetWeatherForecastsRequest.Days).ToList().ForEach
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
    var response = new GetWeatherForecastsResponse(weatherForecastDtos, aGetWeatherForecastsRequest.CorrelationId);

    return Task.FromResult(response);
  }
}
