#region Purpose
// Extends IModule with configuration, middleware, and endpoint hooks so a module can wire the full ASP.NET pipeline, not just DI services.
#endregion

namespace TimeWarp.Foundation;

/// <summary>
/// ASP.NET module that configures configuration sources, middleware, and endpoints in addition to DI.
/// </summary>
public interface IAspNetModule : IModule
{
  /// <summary>
  /// Adds or adjusts configuration sources on the host's <see cref="ConfigurationManager"/>.
  /// </summary>
  static abstract void ConfigureConfiguration(ConfigurationManager configurationManager);
  /// <summary>
  /// Registers middleware in the ASP.NET request pipeline.
  /// </summary>
  static abstract void ConfigureMiddleware(WebApplication webApplication);
  /// <summary>
  /// Maps endpoints on the built <see cref="WebApplication"/>.
  /// </summary>
  static abstract void ConfigureEndpoints(WebApplication webApplication);
}
