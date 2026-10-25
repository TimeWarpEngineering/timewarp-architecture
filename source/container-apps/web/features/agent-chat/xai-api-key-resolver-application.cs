#region Purpose
// Chooses the xAI API key for one Ask turn: host, then the user or the site per policy.
#endregion

#region Design
// The host key is read from IOptionsMonitor, so a configuration change is visible on the next
// call. The mode is read from ISiteSettingsStore on every call, so an admin change is visible
// on the next call. Required does not call the site store. Off does not read a user row. Allow
// reads the user row first and the site row only when the user has none. A missing principal
// skips the user tier. A missing settings store reads as Allow. A settings read that throws is
// logged and treated as Required, so the site key is not used when the mode is unknown. The
// plaintext is returned to the caller and is not logged.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using Microsoft.Extensions.Logging;
using TimeWarp.Identity;

/// <summary>Which tier supplied the key.</summary>
public enum XaiApiKeyOrigin
{
  /// <summary>No key resolved.</summary>
  None = 0,

  /// <summary>Host <c>XAI:ApiKey</c>.</summary>
  Host = 1,

  /// <summary>The signed-in user's key.</summary>
  User = 2,

  /// <summary>The site key from task 398.</summary>
  Site = 3
}

/// <summary>One resolution. <see cref="ApiKey"/> is null when nothing resolved.</summary>
public sealed record ResolvedXaiApiKey(string? ApiKey, XaiByokMode Mode, XaiApiKeyOrigin Origin);

/// <summary>Reads the key Ask should send on this call.</summary>
public interface IXaiApiKeyResolver
{
  /// <summary>Host, then the policy ladder. The result's key may be null.</summary>
  Task<ResolvedXaiApiKey> ResolveAsync(CancellationToken cancellationToken);
}

/// <summary>Host key first. The mode selects the rest.</summary>
public sealed class XaiApiKeyResolver : IXaiApiKeyResolver
{
  private static readonly Action<ILogger, string, Exception?> LogModeUnreadable =
    LoggerMessage.Define<string>
    (
      LogLevel.Warning,
      new EventId(1, nameof(LogModeUnreadable)),
      "xAI BYOK mode could not be read ({ExceptionType})"
    );

  private readonly IOptionsMonitor<XaiChatOptions> Options;
  private readonly ISiteSettingsStore? Settings;
  private readonly IUserXaiApiKey Users;
  private readonly ISiteXaiApiKey Site;
  private readonly ICurrentPrincipalAccessor? CurrentPrincipalAccessor;
  private readonly ILogger<XaiApiKeyResolver>? Logger;

  /// <summary>
  /// Creates the resolver. A missing settings store reads as Allow. A missing principal accessor
  /// skips the user tier, which is the host-then-site path. A missing logger still refuses the
  /// site key when the settings read throws.
  /// </summary>
  public XaiApiKeyResolver(
    IOptionsMonitor<XaiChatOptions> options,
    IUserXaiApiKey users,
    ISiteXaiApiKey site,
    ISiteSettingsStore? settings = null,
    ICurrentPrincipalAccessor? currentPrincipalAccessor = null,
    ILogger<XaiApiKeyResolver>? logger = null)
  {
    Options = options ?? throw new ArgumentNullException(nameof(options));
    Users = users ?? throw new ArgumentNullException(nameof(users));
    Site = site ?? throw new ArgumentNullException(nameof(site));
    Settings = settings;
    CurrentPrincipalAccessor = currentPrincipalAccessor;
    Logger = logger;
  }

  /// <inheritdoc />
  public async Task<ResolvedXaiApiKey> ResolveAsync(CancellationToken cancellationToken)
  {
    XaiByokMode mode = await ReadModeAsync(cancellationToken).ConfigureAwait(false);
    string? host = Options.CurrentValue.ApiKey;
    if (!string.IsNullOrWhiteSpace(host))
    {
      return new ResolvedXaiApiKey(host, mode, XaiApiKeyOrigin.Host);
    }

    if (mode != XaiByokMode.Off)
    {
      Guid? userId = await CurrentUserIdAsync(cancellationToken).ConfigureAwait(false);
      if (userId is Guid id)
      {
        string? userKey = await Users.ReadAsync(id, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(userKey))
        {
          return new ResolvedXaiApiKey(userKey, mode, XaiApiKeyOrigin.User);
        }
      }
    }

    if (mode == XaiByokMode.Required)
    {
      return new ResolvedXaiApiKey(null, mode, XaiApiKeyOrigin.None);
    }

    string? siteKey = await Site.ResolveApiKeyAsync(cancellationToken).ConfigureAwait(false);
    if (!string.IsNullOrWhiteSpace(siteKey))
    {
      return new ResolvedXaiApiKey(siteKey, mode, XaiApiKeyOrigin.Site);
    }

    return new ResolvedXaiApiKey(null, mode, XaiApiKeyOrigin.None);
  }

  private async Task<XaiByokMode> ReadModeAsync(CancellationToken cancellationToken)
  {
    if (Settings is null)
    {
      return XaiByokMode.Allow;
    }

    try
    {
      SiteSettings? settings = await Settings.GetAsync(cancellationToken).ConfigureAwait(false);
      return settings?.XaiByokMode ?? XaiByokMode.Allow;
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
      if (Logger is not null)
      {
        LogModeUnreadable(Logger, exception.GetType().Name, exception);
      }

      // Required skips the site tier. Allow would bill the site when the mode cannot be read.
      return XaiByokMode.Required;
    }
  }

  private async Task<Guid?> CurrentUserIdAsync(CancellationToken cancellationToken)
  {
    if (CurrentPrincipalAccessor is null)
    {
      return null;
    }

    PrincipalId? principalId = await CurrentPrincipalAccessor
      .GetCurrentPrincipalIdAsync(cancellationToken)
      .ConfigureAwait(false);
    return principalId?.Value;
  }
}
