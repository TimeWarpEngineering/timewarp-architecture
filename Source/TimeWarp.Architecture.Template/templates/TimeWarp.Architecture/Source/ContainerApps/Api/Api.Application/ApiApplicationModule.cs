namespace TimeWarp.Architecture.Api.Application;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture;

public class ApiApplicationModule : IModule
{
  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration) => throw new NotImplementedException();
}
