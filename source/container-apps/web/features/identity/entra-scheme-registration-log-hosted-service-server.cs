#region Purpose
// Boot log for the named entra OIDC scheme including the assembly informational version.
#endregion

#region Design
// IHostedLifecycleService.StartingAsync runs before Kestrel accepts requests so a stale build is
// visible on `dev run` without waiting for the first challenge. InformationalVersion is the
// assembly attribute (version plus source revision when the SDK stamps it). Scheme name is
// EntraLinkDefaults.Scheme — never DefaultScheme.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.Reflection;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Configuration;

public sealed class EntraSchemeRegistrationLogHostedService : IHostedLifecycleService
{
  private static readonly Action<ILogger, string, string, Exception?> LogRegistered =
    LoggerMessage.Define<string, string>
    (
      LogLevel.Information,
      new EventId(1, nameof(LogRegistered)),
      "Registered named OpenID Connect scheme {Scheme} (informational version {InformationalVersion})."
    );

  private readonly ILogger<EntraSchemeRegistrationLogHostedService> Logger;

  public EntraSchemeRegistrationLogHostedService(ILogger<EntraSchemeRegistrationLogHostedService> logger)
  {
    Logger = logger;
  }

  public Task StartingAsync(CancellationToken cancellationToken)
  {
    string informationalVersion =
      typeof(EntraAuthenticationRegistration).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion
      ?? "unknown";
    LogRegistered(Logger, EntraLinkDefaults.Scheme, informationalVersion, null);
    return Task.CompletedTask;
  }

  public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
