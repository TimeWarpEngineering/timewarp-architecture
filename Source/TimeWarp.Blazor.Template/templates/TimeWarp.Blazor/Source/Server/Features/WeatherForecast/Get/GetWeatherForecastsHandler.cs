// #WeatherForecast #GetWeatherForecasts #Handler #Server
namespace TimeWarp.Blazor.Features.WeatherForecasts
{
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
      var response = new GetWeatherForecastsResponse(aGetWeatherForecastsRequest.CorrelationId);
      var random = new Random();

      Enumerable.Range(1, aGetWeatherForecastsRequest.Days).ToList().ForEach
      (
        aIndex => response.WeatherForecasts.Add
        (
          new WeatherForecastDto
          (
            aDate: DateTime.Now.AddDays(aIndex),
            aSummary: Summaries[random.Next(Summaries.Length)],
            aTemperatureC: random.Next(-20, 55)
          )
        )
      );

      return Task.FromResult(response);
    }
  }
}
