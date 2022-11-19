namespace TimeWarp.Architecture.Testing;

using Microsoft.Extensions.DependencyInjection;
using System;

public class TimeWarpTestingConvention : TimeWarp.Fixie.TestingConvention
{

  public TimeWarpTestingConvention() : base(ConfigureAdditionalServicesCallback) { }

  private static void ConfigureAdditionalServicesCallback(ServiceCollection serviceCollection)
  {
    Console.WriteLine("ConfigureAdditionalServices");
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
  }
}
