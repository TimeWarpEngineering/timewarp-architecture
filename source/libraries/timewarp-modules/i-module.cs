#region Purpose
// Contract for self-registering modules; the static abstract hook lets each assembly wire its own DI services without a central composition root.
#endregion

namespace TimeWarp.Modules;

public interface IModule
{
  static abstract void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration);
}
