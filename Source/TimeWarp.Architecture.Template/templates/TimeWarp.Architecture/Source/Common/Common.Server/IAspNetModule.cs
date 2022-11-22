namespace TimeWarp.Architecture;

public interface IAspNetModule : IModule
{
  public static abstract void ConfigureConfiguration(ConfigurationManager aConfigurationManager);
  public static abstract void ConfigureMiddleware(WebApplication aWebApplication);
  public static abstract void ConfigureEndpoints(WebApplication aWebApplication);
}
