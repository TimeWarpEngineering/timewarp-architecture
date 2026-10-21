#region Purpose
// Resolves the site xAI API key and applies admin set, replace, and clear.
#endregion

#region Design
// Host configuration wins and is not written to the store. Otherwise the protected-secret row is
// the key Ask uses. Resolve reads it on every call so a change applies without a restart. Audit
// lines name the action (set, replace, clear) and the principal id. They do not include the key
// or the suffix. The plaintext is trimmed before protect. A clear of an empty store still audits,
// because an admin did clear it.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Secrets;

/// <summary>The site xAI API key: host value, else the protected admin secret.</summary>
public interface ISiteXaiApiKey
{
  /// <summary>Status for Admin → AI. Does not unprotect the key.</summary>
  Task<SiteXaiApiKeyStatus> GetStatusAsync(CancellationToken cancellationToken);

  /// <summary>The key Ask should send, or null. Host value wins.</summary>
  Task<string?> ResolveApiKeyAsync(CancellationToken cancellationToken);

  /// <summary>Stores a new key, or a problem that does not contain it.</summary>
  Task<OneOf<SiteXaiApiKeyStatus, SharedProblemDetails>> SetAsync(
    string apiKey,
    Guid setByPrincipalId,
    CancellationToken cancellationToken);

  /// <summary>Removes the stored key, or a problem when the host owns it.</summary>
  Task<OneOf<SiteXaiApiKeyStatus, SharedProblemDetails>> ClearAsync(
    Guid setByPrincipalId,
    CancellationToken cancellationToken);
}

/// <summary>Admin status before the setup hint is attached. No key.</summary>
public sealed record SiteXaiApiKeyStatus(
  bool ManagedByHost,
  bool Configured,
  bool StorageAvailable,
  bool CanChange,
  string? Suffix,
  DateTimeOffset? SetAt,
  Guid? SetByPrincipalId);

/// <summary>Host key first, then <see cref="IProtectedSecretStore"/>.</summary>
public sealed class SiteXaiApiKey : ISiteXaiApiKey
{
  private static readonly Action<ILogger, string, Guid, Exception?> LogChanged =
    LoggerMessage.Define<string, Guid>
    (
      LogLevel.Information,
      new EventId(1, nameof(LogChanged)),
      "Site xAI API key {Action} by {PrincipalId}"
    );

  private readonly IProtectedSecretStore Store;
  private readonly IOptionsMonitor<XaiChatOptions> Options;
  private readonly IHostEnvironment Environment;
  private readonly ILogger<SiteXaiApiKey> Logger;

  /// <summary>Creates the service.</summary>
  public SiteXaiApiKey(
    IProtectedSecretStore store,
    IOptionsMonitor<XaiChatOptions> options,
    IHostEnvironment environment,
    ILogger<SiteXaiApiKey> logger)
  {
    Store = store ?? throw new ArgumentNullException(nameof(store));
    Options = options ?? throw new ArgumentNullException(nameof(options));
    Environment = environment ?? throw new ArgumentNullException(nameof(environment));
    Logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <inheritdoc />
  public async Task<SiteXaiApiKeyStatus> GetStatusAsync(CancellationToken cancellationToken)
  {
    bool host = HostKeyIsSet();
    ProtectedSecretRecord? row = Store.StorageAvailable
      ? await Store.FindAsync(SiteXaiApiKeyNames.Name, cancellationToken).ConfigureAwait(false)
      : null;
    bool adminActive = !host && row is not null;
    return new SiteXaiApiKeyStatus(
      ManagedByHost: host,
      Configured: host || row is not null,
      StorageAvailable: Store.StorageAvailable,
      CanChange: Store.StorageAvailable && !host,
      Suffix: adminActive ? row!.Suffix : null,
      SetAt: adminActive ? row!.SetAt : null,
      SetByPrincipalId: adminActive ? row!.SetByPrincipalId : null);
  }

  /// <inheritdoc />
  public async Task<string?> ResolveApiKeyAsync(CancellationToken cancellationToken)
  {
    string? host = Options.CurrentValue.ApiKey;
    if (!string.IsNullOrWhiteSpace(host))
    {
      return host;
    }

    if (!Store.StorageAvailable)
    {
      return null;
    }

    return await Store.UnprotectAsync(SiteXaiApiKeyNames.Name, SiteXaiApiKeyNames.Purpose, cancellationToken)
      .ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task<OneOf<SiteXaiApiKeyStatus, SharedProblemDetails>> SetAsync(
    string apiKey,
    Guid setByPrincipalId,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(apiKey);
    if (HostKeyIsSet())
    {
      return XaiApiKeyProblems.HostManaged();
    }

    if (!Store.StorageAvailable)
    {
      return XaiApiKeyProblems.StorageUnavailable(Environment.IsDevelopment());
    }

    if (!SiteXaiApiKeyNames.IsAcceptable(apiKey))
    {
      return XaiApiKeyProblems.Rejected();
    }

    ProtectedSecretRecord? existing = await Store.FindAsync(SiteXaiApiKeyNames.Name, cancellationToken)
      .ConfigureAwait(false);
    string trimmed = apiKey.Trim();
    await Store.SetAsync(
      SiteXaiApiKeyNames.Name,
      SiteXaiApiKeyNames.Purpose,
      trimmed,
      setByPrincipalId,
      DateTimeOffset.UtcNow,
      cancellationToken).ConfigureAwait(false);
    LogChanged(Logger, existing is null ? "set" : "replace", setByPrincipalId, null);
    return await GetStatusAsync(cancellationToken).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task<OneOf<SiteXaiApiKeyStatus, SharedProblemDetails>> ClearAsync(
    Guid setByPrincipalId,
    CancellationToken cancellationToken)
  {
    if (HostKeyIsSet())
    {
      return XaiApiKeyProblems.HostManaged();
    }

    if (!Store.StorageAvailable)
    {
      return XaiApiKeyProblems.StorageUnavailable(Environment.IsDevelopment());
    }

    _ = await Store.ClearAsync(SiteXaiApiKeyNames.Name, cancellationToken).ConfigureAwait(false);
    LogChanged(Logger, "clear", setByPrincipalId, null);
    return await GetStatusAsync(cancellationToken).ConfigureAwait(false);
  }

  private bool HostKeyIsSet() => !string.IsNullOrWhiteSpace(Options.CurrentValue.ApiKey);
}
