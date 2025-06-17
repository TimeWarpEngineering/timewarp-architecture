#nullable enable
namespace TimeWarp.Architecture.Testing;

/// <summary>
/// A base testing convention for TimeWarp applications.
/// </summary>
public class TimeWarpTestingConvention : TestingConvention
{

  /// <summary>
  /// Constructor for the TimeWarpTestingConvention
  /// </summary>
  /// <param name="configureAdditionalServicesCallback"> A callback to configure additional services, if any. </param>
  protected TimeWarpTestingConvention(ConfigureAdditionalServicesCallback? configureAdditionalServicesCallback = null)
    : base(serviceCollection => ConfigureServices(serviceCollection, configureAdditionalServicesCallback)) {}

  private static void ConfigureServices(ServiceCollection serviceCollection, ConfigureAdditionalServicesCallback? configureAdditionalServicesCallback)
  {

    // Configure the services for the test application. Aspire might eliminate the need for these registrations.
    serviceCollection
      #if(web)
      .AddSingleton<WebTestServerApplication>()
      #endif
      #if(api)
      .AddSingleton<ApiTestServerApplication>()
      #endif
      #if(yarp)
      .AddSingleton<SpaTestApplication<YarpTestServerApplication, TimeWarp.Architecture.Yarp.Server.Program>>()
      .AddSingleton<YarpTestServerApplication>()
      #endif
      ;

    configureAdditionalServicesCallback?.Invoke(serviceCollection);
  }
}
