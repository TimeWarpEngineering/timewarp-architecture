namespace TimeWarp.Architecture.Services;

[UsedImplicitly]
public class MockApiService : IApiService
{
  public async Task<OneOf<TResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request, CancellationToken cancellationToken
    ) where TResponse : class
  {
    // Based on the request type, return a mock response.
    await Task.Delay(10, cancellationToken);

    if (request is GetWeatherForecasts.Query)
    {
      var response =
          new GetWeatherForecasts.Response
          (
            new GetWeatherForecasts.WeatherForecastDto[]
            {
              new(
                date: DateTime.Now.AddDays(1),
                summary: "Summary 1",
                temperatureC: 25
              ),
            }
          );
      return (response as TResponse)!;
    }
    throw new NotImplementedException();
  }
}
