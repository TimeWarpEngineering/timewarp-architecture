namespace TimeWarp.Architecture;

using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class CommonServerModule : IAspNetModule
{
  public static void ConfigureConfiguration(ConfigurationManager aConfigurationManager) { }
  public static void ConfigureEndpoints(IEndpointRouteBuilder aEndpointRouteBuilder, IServiceProvider aServiceCollection) { }
  public static void ConfigureMiddleware(WebApplication aWebApplication, IServiceProvider aServiceCollection, IHostEnvironment aHostEnvironment) { }
  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    ValidatorOptions.Global.DisplayNameResolver =
      (aType, aMemberInfo, aLambdaExpression) =>
        aType != null && aMemberInfo != null ? $"{aType.Name}:{aMemberInfo.Name}" : null;
  }
}
