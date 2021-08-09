namespace TimeWarp.Blazor.Features.WeatherForecastGrpc
{
  using ProtoBuf.Grpc;
  using System.Linq;
  using System;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.WeatherForecastsGrpc;

  public class WeatherForecastGrpcService : IWeatherForecastService
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

    public Task<GetWeatherForecastsResponse> GetWeatherForecastsAsync
    (
      GetWeatherForecastsRequest aGetWeatherForecastsRequest,
      CallContext aCallContext = default
    )
    {
      var response = new GetWeatherForecastsResponse();
      var random = new Random();

      Enumerable.Range(1, aGetWeatherForecastsRequest.Days).ToList().ForEach
      (
        aIndex => response.WeatherForecasts.Add
        (
          new GetWeatherForecastsResponse.WeatherForecastDto
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
