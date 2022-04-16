namespace TimeWarp.Architecture;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public interface IModule
{
  public static abstract void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration);

}
