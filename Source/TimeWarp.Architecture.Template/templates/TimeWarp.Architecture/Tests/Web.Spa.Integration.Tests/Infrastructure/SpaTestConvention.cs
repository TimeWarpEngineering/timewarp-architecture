namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Testing;

class SpaTestConvention : TimeWarpTestingConvention
{

  private static void ConfigureAdditionalServicesCallback(ServiceCollection serviceCollection)
  {
    serviceCollection.AddSingleton<SpaTestApplication<YarpTestServerApplication, Yarp.Server.Program>>(); ;
    // One would configure their Appplication Objects here as well as any other test services
  }

  
}
