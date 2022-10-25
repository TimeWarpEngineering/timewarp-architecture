namespace TimeWarp.Architecture.Testing;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Used to launch the Api.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesDelegate"/></remarks>
public class ApiTestServerApplication : TestServerApplication<Api.Server.Program>
{
  public ApiTestServerApplication() :
    base
    (
      new WebApplicationHost<Api.Server.Program>
      (
        aUrls: new[]
        {
          "https://localhost:7255"
        },
        aWebApplicationOptions:
        new WebApplicationOptions
        {
          ApplicationName = typeof(Api_Server_Assembly).Assembly.GetName().Name,
          EnvironmentName = Environments.Development,
          ContentRootPath = default,
        },
        ConfigureServicesCallback
      )
    )
  { }

  protected static void ConfigureServicesCallback(IServiceCollection aServiceCollection) { }
}
