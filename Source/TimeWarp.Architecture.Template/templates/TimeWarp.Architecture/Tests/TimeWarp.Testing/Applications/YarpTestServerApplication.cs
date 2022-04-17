﻿namespace TimeWarp.Architecture.Testing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Used to launch the Api.Server application
/// </summary>
/// <remarks>One can override the configuration for testing by updating the <see cref="ConfigureServicesDelegate"/></remarks>
public class YarpTestServerApplication : TestServerApplication<Yarp.Server.Program>
{
  private readonly WebTestServerApplication WebTestServerApplication;
  private readonly ApiTestServerApplication ApiTestServerApplication;
  public YarpTestServerApplication
  (
    WebTestServerApplication aWebTestServerApplication,
    ApiTestServerApplication aApiTestServerApplication
  ) :
  base
  (
    new WebApplicationHost<Yarp.Server.Program>
    (
      aEnvironmentName: Environments.Development,
      aUrls: new[]
      {
        "https://localhost:8443"
      },
      ConfigureServicesDelegate
    )
  )
  {
    WebTestServerApplication = aWebTestServerApplication;
    ApiTestServerApplication = aApiTestServerApplication;
  }

  protected static void ConfigureServicesDelegate
  (
    HostBuilderContext aHostBuilderContext,
    IServiceCollection aServiceCollection
  )
  { }
}