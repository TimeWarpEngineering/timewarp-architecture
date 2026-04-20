namespace TimeWarp.Modules;

public interface IModule
{
  static abstract void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration);
}
