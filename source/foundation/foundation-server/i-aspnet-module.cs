#region Purpose
// Extends IModule with configuration, middleware, and endpoint hooks so a module can wire the full ASP.NET pipeline, not just DI services.
#endregion

namespace TimeWarp.Foundation;

public interface IAspNetModule : IModule
{
  static abstract void ConfigureConfiguration(ConfigurationManager configurationManager);
  static abstract void ConfigureMiddleware(WebApplication webApplication);
  static abstract void ConfigureEndpoints(WebApplication webApplication);
}
