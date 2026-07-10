namespace TimeWarp.Architecture.Testing;

/// <summary>
/// Used to launch the Api.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesDelegate"/></remarks>
public sealed class ApiTestServerApplication : TestServerApplication<Api.Server.Program>
{
  private const string ApiHostUrl = "https://localhost:7255";

  public ApiTestServerApplication() :
    base
    (
      new WebApplicationHost<Api.Server.Program>
      (
        urls:
        [
          ApiHostUrl
        ],
        webApplicationOptions:
        new WebApplicationOptions
        {
          ApplicationName = typeof(TimeWarp.Architecture.Api.Server.IAssemblyMarker).Assembly.GetName().Name,
          EnvironmentName = Environments.Development,
          ContentRootPath = default,
        },
        ConfigureServicesCallback
      )
    )
  { }

  private static void ConfigureServicesCallback(IServiceCollection serviceCollection)
  {
    Uri webServiceUri = ServiceUriHelper.GetServiceHttpsUri(ServiceNames.WebServiceName) ?? new Uri(ApiHostUrl);
    serviceCollection.AddHttpClient(ServiceNames.WebServiceName, client => client.BaseAddress = webServiceUri);
  }

  protected override IWebApiTestService CreateWebApiTestService(WebApplicationHost<Api.Server.Program> webApplicationHost) =>
    new WebApiTestService(new TestApiService(HttpClient, ContractSerializationDefaults.Options));
}
