#region Purpose
// DI convention for api-server integration tests: aspire-launched app + a test client bound to it.
#endregion

#region Design
// The client is the test-owned TestApiService (timewarp-testing), not the SPA's ApiServerApiService:
// the api test suite must work in template combinations where the web feature (and with it the SPA
// client stack) is excluded. A former IWebServerApiService registration had no consumers and was
// dropped rather than ported.
#endregion

namespace TimeWarp.Architecture.Api.Server.Integration.Tests.Infrastructure;

using global::Aspire.Hosting;
using System.Text.Json;
using AspireConstants = TimeWarp.Architecture.Aspire.Constants;

class ApiServerTestConvention : TimeWarpTestingConvention
{
  public ApiServerTestConvention() : base(ConfigureServices) {}
  private static void ConfigureServices(ServiceCollection serviceCollection)
  {
    // Register a factory method for DistributedApplication
    serviceCollection.AddSingleton
    (
      async _ =>
      {
        IDistributedApplicationTestingBuilder appHost =
          await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>
          (
            // Ephemeral postgres: test AppHosts must NOT share the deterministic data volume
            // (overlapping instances corrupt its WAL and hang WaitFor - see AppHost Design region).
            ["--Postgres:UseDataVolume=false"]
          );

        DistributedApplication app = await appHost.BuildAsync();
        await app.StartAsync();
        return app;
      }
    );

    // Register the canonical contract-seam JsonSerializerOptions (was default/PascalCase — a
    // latent mismatch with the camelCase seam).
    serviceCollection.AddSingleton(ContractSerializationDefaults.Options);

    // Register the api-server test client
    serviceCollection.AddScoped<TestApiService>(provider =>
    {
      Task<DistributedApplication> distributedAppTask = provider.GetRequiredService<Task<DistributedApplication>>();
      DistributedApplication distributedApp = distributedAppTask.Result; // Ensure the app is available
      HttpClient httpClient = distributedApp.CreateHttpClient(AspireConstants.ApiServerProjectResourceName);
      JsonSerializerOptions jsonSerializerOptions = provider.GetRequiredService<JsonSerializerOptions>();
      return new TestApiService(httpClient, jsonSerializerOptions);
    });
  }
}
