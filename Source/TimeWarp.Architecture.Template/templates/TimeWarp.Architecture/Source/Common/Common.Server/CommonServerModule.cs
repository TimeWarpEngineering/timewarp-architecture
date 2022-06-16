namespace TimeWarp.Architecture;

using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class CommonServerModule : IAspNetModule
{
  public static void ConfigureConfiguration(ConfigurationManager aConfigurationManager) { }
  public static void ConfigureEndpoints(WebApplication aWebApplication)
  {
    var configurationRoot = aWebApplication!.Configuration as IConfigurationRoot;

    if (aWebApplication.Environment.IsDevelopment())
    {
      aWebApplication.MapGet
      (
        "/api/debug-config",
        aHttpContext =>
        {
          string? config = configurationRoot.GetDebugView();
          return aHttpContext.Response.WriteAsync(config);
        }
      );
    }
  }
  public static void ConfigureMiddleware(WebApplication aWebApplication) { }
  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    ValidatorOptions.Global.DisplayNameResolver =
      (aType, aMemberInfo, aLambdaExpression) =>
        aType != null && aMemberInfo != null ? $"{aType.Name}:{aMemberInfo.Name}" : null;
  }
}
