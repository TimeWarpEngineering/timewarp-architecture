namespace GetWeatherForecastsEndpoint_Aspire_;

using System.Text.Json;
using TimeWarp.Architecture.Services;
using static TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts;

public class Returns
{
  private readonly IApiServerApiService ApiServerApiService;
  private readonly Query Query = new()
  {
    Days = 10
  };

  public Returns( IApiServerApiService apiServerApiService)
  {
    ApiServerApiService = apiServerApiService;
  }
  public async Task _10WeatherForecasts_Given_10DaysRequested()
  {

    OneOf<Response, FileResponse, SharedProblemDetails> response =
      await ApiServerApiService.GetResponse<Response>(Query, new CancellationToken());

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

    OneOf<Response, FileResponse, SharedProblemDetails> response =
      await ApiServerApiService.GetResponse<Response>(Query, new CancellationToken());

    // Validate the response
    response.Switch
    (
      _ => throw new Exception("Received a response but expectedSharedProblemDetails "),
      _ => throw new Exception("Received a file response but expectedSharedProblemDetails "),
      ConfirmEndpointValidationError
    );
  }

  private void ValidateGetWeatherForecastsResponse(Response getWeatherForecastsResponse)
  {
    getWeatherForecastsResponse.WeatherForecasts.Count().Should().Be(Query.Days);
  }

  private void ConfirmEndpointValidationError(SharedProblemDetails sharedProblemDetails)
  {
    sharedProblemDetails.Status.Should().Be(400);
    sharedProblemDetails.Extensions.Count().Should().Be(2);

    sharedProblemDetails.Title.Should().Be("One or more validation errors occurred.");
    sharedProblemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.1");

    // Deserialize the JSON content in sharedProblemDetails.Extensions["errors"]
    string errorsJson = sharedProblemDetails.Extensions["errors"].ToString();
    Dictionary<string, List<string>> errors = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(errorsJson);

    // Validate the structure and values of the deserialized object
    errors.Should().ContainKey("Days");
    errors["Days"].Should().ContainSingle()
        .Which.Should().Be("'Query:Days' must be greater than '0'.");

  }
}
