namespace TimeWarp.Architecture.Api.Server.Integration.Tests.Infrastructure;

using global::Aspire.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Services;
using System.Text.Json;

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
          await DistributedApplicationTestingBuilder.CreateAsync<Projects.Aspire_AppHost>();

        DistributedApplication app = await appHost.BuildAsync();
        await app.StartAsync();
        return app;
      }
    );

    // Register JsonSerializerOptions
    serviceCollection.AddSingleton(new JsonSerializerOptions());

    // Register the access token provider
    serviceCollection.AddSingleton<IAccessTokenProvider, MockAccessTokenProvider>();

    // Register IApiServerApiService
    serviceCollection.AddScoped<IApiServerApiService, ApiServerApiService>(provider =>
    {
      Task<DistributedApplication> distributedAppTask = provider.GetRequiredService<Task<DistributedApplication>>();
      DistributedApplication distributedApp = distributedAppTask.Result; // Ensure the app is available
      HttpClient httpClient = distributedApp.CreateHttpClient("api-server");
      JsonSerializerOptions jsonSerializerOptions = provider.GetRequiredService<JsonSerializerOptions>();
      IAccessTokenProvider accessTokenProvider = provider.GetRequiredService<IAccessTokenProvider>();
      return new ApiServerApiService(httpClient, accessTokenProvider, jsonSerializerOptions);
    });

    // Register IWebServerApiService
    serviceCollection.AddScoped<IWebServerApiService, WebServerApiService>(provider =>
    {
      Task<DistributedApplication> distributedAppTask = provider.GetRequiredService<Task<DistributedApplication>>();
      DistributedApplication distributedApp = distributedAppTask.Result; // Ensure the app is available
      IAccessTokenProvider accessTokenProvider = provider.GetRequiredService<IAccessTokenProvider>();
      HttpClient httpClient = distributedApp.CreateHttpClient("web-server");
      JsonSerializerOptions jsonSerializerOptions = provider.GetRequiredService<JsonSerializerOptions>();
      return new WebServerApiService(accessTokenProvider, httpClient, jsonSerializerOptions);
    });
  }
}
