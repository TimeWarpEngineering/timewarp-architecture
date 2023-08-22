namespace GetWeatherForecastsEndpoint_;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

public class Returns
{
  private readonly Query Query;
  private readonly ApiTestServerApplication ApiTestServerApplication;

  public Returns
  (
    ApiTestServerApplication aApiTestServerApplication
  )
  {
    Query = new Query { Days = 10 };
    ApiTestServerApplication = aApiTestServerApplication;
  }

  public async Task _10WeatherForecasts_Given_10DaysRequested()
  {
    Response response =
      await ApiTestServerApplication.GetResponse<Response>(Query);

    ValidateGetWeatherForecastsResponse(response);
  }

  public async Task ValidationError()
  {
    Query.Days = -1;

    await ApiTestServerApplication.ConfirmEndpointValidationError<Response>(Query, nameof(Query.Days));
  }

  private void ValidateGetWeatherForecastsResponse(Response aGetWeatherForecastsResponse)
  {
    aGetWeatherForecastsResponse.WeatherForecasts.Count().Should().Be(Query.Days);
  }
}
