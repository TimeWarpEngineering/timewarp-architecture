namespace GetWeatherForecastsEndpoint_;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

public class Returns
(
  ApiTestServerApplication apiTestServerApplication
)
{
  private readonly Query Query = new() { Days = 10 };

  public async Task _10WeatherForecasts_Given_10DaysRequested()
  {
    OneOf<Response, FileResponse, SharedProblemDetails> response =
      await apiTestServerApplication.GetResponse<Response>(Query, new CancellationToken());

    // Validate the response
    response.Switch
    (
      ValidateGetWeatherForecastsResponse,
      _ => throw new Exception("File response returned"),
      _ => throw new Exception("Problem details returned")
    );
  }

  public async Task ValidationError()
  {
    Query.Days = -1;

    await apiTestServerApplication.ConfirmEndpointValidationError<Response>(Query, nameof(Query.Days));
  }

  private void ValidateGetWeatherForecastsResponse(Response getWeatherForecastsResponse)
  {
    getWeatherForecastsResponse.WeatherForecasts.Count().Should().Be(Query.Days);
  }
}
