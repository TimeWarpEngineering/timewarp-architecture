namespace TimeWarp.Architecture;

public interface IModule
{
  public static abstract void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration);

}
