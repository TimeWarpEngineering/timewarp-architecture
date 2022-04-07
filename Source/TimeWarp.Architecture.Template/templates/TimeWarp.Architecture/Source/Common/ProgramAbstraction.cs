namespace TimeWarp.Architecture;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public interface IProgram
{
  public static abstract void ConfigureConfiguration(ConfigurationManager aConfigurationManager);
  public static abstract void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration);
  public static abstract void ConfigureMiddleware(WebApplication aWebApplication, IServiceProvider aServiceCollection, IHostEnvironment aHostEnvironment);
  public static abstract void ConfigureEndpoints(IEndpointRouteBuilder aEndpointRouteBuilder, IServiceProvider aServiceCollection);
}
