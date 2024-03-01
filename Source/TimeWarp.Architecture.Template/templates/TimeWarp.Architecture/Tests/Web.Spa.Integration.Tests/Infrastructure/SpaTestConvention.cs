namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

class SpaTestConvention : TimeWarpTestingConvention
{

  private static void ConfigureAdditionalServicesCallback(ServiceCollection serviceCollection)
  {
    serviceCollection.AddSingleton<SpaTestApplication<YarpTestServerApplication, Yarp.Server.Program>>(); ;
    // One would configure their Application Objects here as well as any other test services
  }
}
