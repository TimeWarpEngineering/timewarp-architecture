namespace TimeWarp.Architecture;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

public interface IProgram
{
  public static abstract void ConfigureServices(IServiceCollection aServiceCollection);
  public static abstract void ConfigureMiddleware(IApplicationBuilder aApplicationBuilder, IServiceProvider aServiceCollection);
  public static abstract void ConfigureEndpoints(IEndpointRouteBuilder aEndpointRouteBuilder, IServiceProvider aServiceCollection);
}
