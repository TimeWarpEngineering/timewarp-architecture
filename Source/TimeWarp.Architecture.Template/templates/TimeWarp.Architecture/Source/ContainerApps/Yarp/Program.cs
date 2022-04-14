namespace TimeWarp.Architecture.Yarp.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

public class Program : IAspNetProgram
{
  public static Task Main(string[] aArgumentArray)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(aArgumentArray);
    ConfigureConfiguration(builder.Configuration);
    ConfigureServices(builder.Services, builder.Configuration);

    WebApplication webApplication = builder.Build();

    ConfigureMiddleware(webApplication, webApplication.Services, webApplication.Environment);
    ConfigureEndpoints(webApplication, webApplication.Services);

    return webApplication.RunAsync();
  }
  public static void ConfigureConfiguration(ConfigurationManager aConfigurationManager) { }
  public static void ConfigureEndpoints
  (
    IEndpointRouteBuilder aEndpointRouteBuilder,
    IServiceProvider aServiceCollection
  )
  { }

  public static void ConfigureMiddleware
  (
    WebApplication aWebApplication,
    IServiceProvider aServiceCollection,
    IHostEnvironment aHostEnvironment
  ) => aWebApplication.MapReverseProxy();

  public static void ConfigureServices
  (
    IServiceCollection aServiceCollection,
    IConfiguration aConfiguration
  )
  {
    aServiceCollection
      .AddReverseProxy()
      .LoadFromConfig(aConfiguration.GetSection("ReverseProxy"));
  }
}
