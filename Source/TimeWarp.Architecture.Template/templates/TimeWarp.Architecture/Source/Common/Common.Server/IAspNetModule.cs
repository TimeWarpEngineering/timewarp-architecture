namespace TimeWarp.Architecture;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public interface IAspNetModule : IModule
{
  public static abstract void ConfigureConfiguration(ConfigurationManager aConfigurationManager);
  public static abstract void ConfigureMiddleware(WebApplication aWebApplication, IServiceProvider aServiceCollection, IHostEnvironment aHostEnvironment);
  public static abstract void ConfigureEndpoints(IEndpointRouteBuilder aEndpointRouteBuilder, IServiceProvider aServiceCollection);
}
