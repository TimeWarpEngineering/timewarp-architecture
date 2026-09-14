#region Purpose
// One-version obsolete log when Authentication:UseEntra mapped to Authentication:Entra:Enabled.
#endregion

#region Design
// The synonym must never restore DefaultScheme Entra (RFC 219 D10). Logging happens when options
// are first materialized so ConfigureServices does not need an ILogger. Once per process.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Features.Identity.Application;

public sealed class EntraObsoleteKeyPostConfigure : IPostConfigureOptions<EntraAuthenticationOptions>
{
  private static readonly Action<ILogger, Exception?> LogObsoleteUseEntra =
    LoggerMessage.Define(
      LogLevel.Warning,
      new EventId(1, nameof(LogObsoleteUseEntra)),
      "Authentication:UseEntra is obsolete; use Authentication:Entra:Enabled. The obsolete key never selects Entra as DefaultScheme.");

  private static int Logged;
  private readonly ILogger<EntraObsoleteKeyPostConfigure> Logger;

  public EntraObsoleteKeyPostConfigure(ILogger<EntraObsoleteKeyPostConfigure> logger)
  {
    Logger = logger;
  }

  public void PostConfigure(string? name, EntraAuthenticationOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);
    if (!options.UsedObsoleteUseEntraKey || Interlocked.Exchange(ref Logged, 1) != 0)
    {
      return;
    }

    LogObsoleteUseEntra(Logger, null);
  }
}
