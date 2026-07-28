#region Purpose
// Host-level guard: Development api-server serves /openapi/v1.json with feature tags for Scalar.
#endregion

#region Design
// Uses the Aspire-launched api-server (TestApiService stack) rather than in-process
// ApiTestServerApplication: the test process loads web-server via timewarp-testing, and
// FastEndpoints endpoint discovery can see web types in the same AppDomain, polluting an
// in-process document. Aspire runs api-server as its own process — same surface the manual
// curl proof used. ASPNETCORE_ENVIRONMENT is Development under the testing AppHost so
// CommonServerModule.UseScalarApiReference maps /openapi/{doc}.json.
// Asserts HTTP 200 and that at least one operation carries the generator-emitted feature tag
// (namespace leaf …Features.WeatherForecasts → "WeatherForecasts") — the original failure mode
// was Scalar UI up with an empty/untagged document.
#endregion

namespace OpenApiDocument_;

using global::Aspire.Hosting;
using System.Text.Json;
using AspireConstants = TimeWarp.Architecture.Aspire.Constants;

public class Returns
{
  private readonly Task<DistributedApplication> DistributedApplicationTask;

  public Returns(Task<DistributedApplication> distributedApplicationTask)
  {
    DistributedApplicationTask = distributedApplicationTask;
  }

  public async Task OpenApi_V1_Document_With_Feature_Tags()
  {
    DistributedApplication app = await DistributedApplicationTask;
    using HttpClient httpClient = app.CreateHttpClient(AspireConstants.ApiServerProjectResourceName);

    HttpResponseMessage response = await httpClient.GetAsync("/openapi/v1.json");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    string json = await response.Content.ReadAsStringAsync();
    using JsonDocument document = JsonDocument.Parse(json);

    bool hasWeatherForecastsTag = false;
    if (document.RootElement.TryGetProperty("paths", out JsonElement paths))
    {
      foreach (JsonProperty path in paths.EnumerateObject())
      {
        foreach (JsonProperty operation in path.Value.EnumerateObject())
        {
          if (!operation.Value.TryGetProperty("tags", out JsonElement tags))
          {
            continue;
          }

          foreach (JsonElement tag in tags.EnumerateArray())
          {
            if (tag.GetString() == "WeatherForecasts")
            {
              hasWeatherForecastsTag = true;
              break;
            }
          }

          if (hasWeatherForecastsTag)
          {
            break;
          }
        }

        if (hasWeatherForecastsTag)
        {
          break;
        }
      }
    }

    hasWeatherForecastsTag.ShouldBeTrue(
      "OpenAPI document should tag at least one operation with WeatherForecasts (generator Description.WithTags from …Features.WeatherForecasts).");
  }
}
