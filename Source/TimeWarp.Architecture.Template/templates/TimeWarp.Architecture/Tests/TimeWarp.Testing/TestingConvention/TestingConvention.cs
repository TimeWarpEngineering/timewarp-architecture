namespace TimeWarp.Fixie.Tests;

using Microsoft.Extensions.DependencyInjection;
using System;
using TimeWarp.Architecture.Testing;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

public class TimeWarpTestingConvention : TimeWarp.Fixie.TestingConvention
{

  public TimeWarpTestingConvention() : base(ConfigureAdditionalServicesCallback) { }

  private static void ConfigureAdditionalServicesCallback(ServiceCollection serviceCollection)
  {
    Console.WriteLine("ConfigureAdditionalServices");
    serviceCollection
      .AddSingleton<WebTestServerApplication>()
      .AddSingleton<ApiTestServerApplication>()
      .AddSingleton<YarpTestServerApplication>();
  }
}
