namespace TimeWarp.Architecture;

public interface IAspNetModule : IModule
{
  static abstract void ConfigureConfiguration(ConfigurationManager configurationManager);
  static abstract void ConfigureMiddleware(WebApplication webApplication);
  static abstract void ConfigureEndpoints(WebApplication webApplication);
}
