namespace GetWeatherForecastsEndpoint_;

using System.Linq;
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
    getWeatherForecastsResponse.WeatherForecasts.Count().ShouldBe(Query.Days!.Value);
  }

  private void ConfirmEndpointValidationError(SharedProblemDetails sharedProblemDetails)
  {
    sharedProblemDetails.Status.ShouldBe(400);

  sharedProblemDetails.Title.ShouldBe("One or more validation errors occurred");
  sharedProblemDetails.Type.ShouldBe("https://tools.ietf.org/html/rfc7231#section-6.5.1");

    sharedProblemDetails.Extensions.ShouldContainKey("errors");

    // Deserialize the JSON content in sharedProblemDetails.Extensions["errors"]
    string errorsJson = sharedProblemDetails.Extensions["errors"]?.ToString() ?? string.Empty;
    Dictionary<string, List<string>> errors = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(errorsJson) ?? [];

  // Validate the structure and values of the deserialized object
  KeyValuePair<string, List<string>> daysError = errors.Single(kvp => kvp.Key.Contains("Days", StringComparison.OrdinalIgnoreCase));
  string errorMessage = daysError.Value.ShouldHaveSingleItem();
  string normalizedMessage = errorMessage.ToLowerInvariant();
  normalizedMessage.ShouldContain("greater than");
  normalizedMessage.ShouldContain("1");

  }
}
