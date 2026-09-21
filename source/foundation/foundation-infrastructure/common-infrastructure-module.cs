#region Purpose
// Registers infrastructure services every server needs (current-user resolution) so each host wires them through one module instead of repeating the registrations.
#endregion

namespace TimeWarp.Foundation.Common.Infrastructure;

/// <summary>
/// Registers shared infrastructure services (current-user resolution) for every server host.
/// </summary>
public class CommonInfrastructureModule : IModule
{
  /// <summary>
  /// Adds scoped <see cref="ICurrentUserService"/> → <see cref="CurrentUserService"/>.
  /// </summary>
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.TryAddScoped<ICurrentUserService, CurrentUserService>();
  }
}
