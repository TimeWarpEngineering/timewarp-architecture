namespace GetWeatherForecastsHandler;

using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;
public class Handle_Returns
{
  private readonly Query Query;
  private readonly ApiTestServerApplication ApiTestServerApplication;

  public Handle_Returns
  (
     ApiTestServerApplication aApiTestServerApplication
  )
  {
    Query = new Query { Days = 10 };
    ApiTestServerApplication = aApiTestServerApplication;
  }

  public async Task _10WeatherForecasts_Given_10DaysRequested()
  {
    OneOf<Response, SharedProblemDetails> result = await ApiTestServerApplication.Send(Query);

    ValidateResult(result);
  }

  private void ValidateResult(OneOf<Response, SharedProblemDetails> result)
  {
    result.Switch
    (
      response =>
      {
        response.ShouldNotBeNull();
        response.WeatherForecasts.Count().ShouldBe(Query.Days!.Value);
      },
      problemDetails =>
      {
        // This should not happen in a successful case
        problemDetails.ShouldBeNull("The SignIn handler returned SharedProblemDetails instead of a successful response.");
      }
    );
  }
}
