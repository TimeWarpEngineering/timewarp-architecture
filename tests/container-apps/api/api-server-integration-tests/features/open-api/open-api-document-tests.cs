#region Purpose
// Host-level guard: Development api-server serves /openapi/v1.json with feature tags for Scalar.
#endregion

#region Design
// Closed-box lane (task 145-005): Aspire-launched api-server rather than in-process
// ApiTestServerApplication — the test process used to load web-server via timewarp-testing, and
// FastEndpoints endpoint discovery can see web types in the same AppDomain, polluting an
// in-process document. Aspire runs api-server as its own process. SetupOnce owns the
// DistributedApplication (145-003 Jaribu shape). ASPNETCORE_ENVIRONMENT is Development under
// the testing AppHost so CommonServerModule.UseScalarApiReference maps /openapi/{doc}.json.
// Asserts HTTP 200 and that at least one operation carries the generator-emitted feature tag
// (namespace leaf …Features.WeatherForecasts → "WeatherForecasts").
#endregion

namespace OpenApiDocument_;

using System.Text.Json;
using AspireConstants = TimeWarp.Architecture.Aspire.Constants;

[TestTag("Integration")]
public class OpenApiDocument_Given_
{
  private static DistributedApplication? App;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<OpenApiDocument_Given_>();

  public static async Task SetupOnce()
  {
    IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>
      (
        // Ephemeral postgres: test AppHosts must NOT share the deterministic data volume
        // (overlapping instances corrupt its WAL and hang WaitFor - see AppHost Design region).
        ["--Postgres:UseDataVolume=false"]
      );

    App = await appHost.BuildAsync();
    await App.StartAsync();

    using CancellationTokenSource cts = new(TimeSpan.FromMinutes(2));
    await App.ResourceNotifications.WaitForResourceHealthyAsync(
      AspireConstants.ApiServerProjectResourceName,
      cts.Token);
  }

  public static async Task CleanUpOnce()
  {
    if (App is not null)
    {
      await App.DisposeAsync();
      App = null;
    }
  }

  public static async Task OpenApi_V1_Document_Should_Include_WeatherForecasts_Feature_Tag()
  {
    using HttpClient httpClient = App!.CreateHttpClient(AspireConstants.ApiServerProjectResourceName);

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
