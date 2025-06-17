namespace TimeWarp.Architecture;

public interface IAspNetModule : IModule
{
  static abstract void ConfigureConfiguration(ConfigurationManager aConfigurationManager);
  static abstract void ConfigureMiddleware(WebApplication aWebApplication);
  static abstract void ConfigureEndpoints(WebApplication aWebApplication);
}
