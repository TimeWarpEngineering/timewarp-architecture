#region Purpose
// Reads the web-server assembly's informational version once and returns it for every caller.
#endregion

#region Design
// The web-server assembly (Web.Server.IAssemblyMarker) is the app's entry point in production;
// Assembly.GetEntryAssembly is not used because a test host is the entry assembly under
// WebApplicationFactory. The attribute does not change while the process runs, so the value is
// read once. Registered as a singleton by Web.Server's Program.ConfigureServices.
#endregion

namespace TimeWarp.Architecture.Versioning;

using System.Reflection;

public sealed class AssemblyAppVersionAccessor : IAppVersionAccessor
{
  private static readonly string? InformationalVersion =
    typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker).Assembly
      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
      ?.InformationalVersion;

  public string? GetVersion() => InformationalVersion;
}
