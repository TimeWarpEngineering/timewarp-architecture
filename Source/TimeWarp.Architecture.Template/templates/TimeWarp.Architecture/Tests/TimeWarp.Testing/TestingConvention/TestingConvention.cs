namespace TimeWarp.Architecture.Testing;

using Microsoft.Extensions.DependencyInjection;
using System;
using TimeWarp.Architecture.Testing;

public class TimeWarpTestingConvention : TimeWarp.Fixie.TestingConvention
{

  public TimeWarpTestingConvention() : base(ConfigureAdditionalServicesCallback) { }

  private static void ConfigureAdditionalServicesCallback(ServiceCollection serviceCollection)
  {
    Console.WriteLine("ConfigureAdditionalServices");
    serviceCollection
      .AddSingleton<SpaTestApplication<YarpTestServerApplication, TimeWarp.Architecture.Yarp.Server.Program>>()
      .AddSingleton<WebTestServerApplication>()
      .AddSingleton<ApiTestServerApplication>()
      .AddSingleton<YarpTestServerApplication>();
  }
}
