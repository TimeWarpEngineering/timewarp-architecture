#region Purpose
// Contract for self-registering modules; the static abstract hook lets each assembly wire its own DI services without a central composition root.
#endregion

namespace TimeWarp.Modules;

/// <summary>
/// Self-registering module contract. Each assembly implements the static abstract hook so it can
/// wire its own DI services without a central composition root.
/// </summary>
public interface IModule
{
  /// <summary>Registers this module's services into the host container.</summary>
  static abstract void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration);
}
