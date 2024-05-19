namespace TimeWarp.Architecture;

public interface IModule
{
  static abstract void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration);

}
