#region Purpose
// Resolves the site xAI API key and applies admin set, replace, and clear.
#endregion

#region Design
// Host configuration wins and is not written to the store. Otherwise the protected-secret row is
// the key Ask uses. Resolve reads it on every call so a change applies without a restart. A store
// failure (missing table, database down) returns null from resolve, and 503 from status, set, and
// clear. The log names the exception type, never the key. Configured is a trial unprotect: a row
// that cannot be read is NeedsReplacement, not configured. Audit lines name the action (set,
// replace, clear) and the principal id. Set and replace come from the store's write result, so a
// concurrent first insert is audited as replace. They do not include the key or the suffix. The
// plaintext is trimmed before protect. A clear of an empty store still audits, because an admin
// did clear it.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Secrets;

/// <summary>The site xAI API key: host value, else the protected admin secret.</summary>
public interface ISiteXaiApiKey
{
  /// <summary>False when this host cannot store a secret that survives a restart.</summary>
  bool StorageAvailable { get; }

  /// <summary>Status for Admin → AI. A trial unprotect decides Configured. The plaintext is not returned. 503 when the store throws.</summary>
  Task<OneOf<SiteXaiApiKeyStatus, SharedProblemDetails>> GetStatusAsync(CancellationToken cancellationToken);

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
  bool NeedsReplacement,
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

  private static readonly Action<ILogger, string, Exception?> LogStorageFailed =
    LoggerMessage.Define<string>
    (
      LogLevel.Warning,
      new EventId(2, nameof(LogStorageFailed)),
      "Site xAI API key storage failed ({ExceptionType})"
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
  public bool StorageAvailable => Store.StorageAvailable;

  /// <inheritdoc />
  public async Task<OneOf<SiteXaiApiKeyStatus, SharedProblemDetails>> GetStatusAsync(CancellationToken cancellationToken)
  {
    bool host = HostKeyIsSet();
    if (host || !Store.StorageAvailable)
    {
      return IdleStatus(host);
    }

    try
    {
      ProtectedSecretRecord? row = await Store.FindAsync(SiteXaiApiKeyNames.Name, cancellationToken)
        .ConfigureAwait(false);
      string? plain = row is null
        ? null
        : await Store.UnprotectAsync(SiteXaiApiKeyNames.Name, SiteXaiApiKeyNames.Purpose, cancellationToken)
          .ConfigureAwait(false);
      bool readable = row is not null && !string.IsNullOrWhiteSpace(plain);
      return new SiteXaiApiKeyStatus(
        ManagedByHost: false,
        Configured: readable,
        StorageAvailable: true,
        CanChange: true,
        NeedsReplacement: row is not null && !readable,
        Suffix: row?.Suffix,
        SetAt: row?.SetAt,
        SetByPrincipalId: row?.SetByPrincipalId);
    }
    catch (Exception exception) when (IsStorageFailure(exception))
    {
      LogStorageFailed(Logger, exception.GetType().Name, exception);
      return XaiApiKeyProblems.StorageFailed();
    }
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

    try
    {
      return await Store.UnprotectAsync(SiteXaiApiKeyNames.Name, SiteXaiApiKeyNames.Purpose, cancellationToken)
        .ConfigureAwait(false);
    }
    catch (Exception exception) when (IsStorageFailure(exception))
    {
      LogStorageFailed(Logger, exception.GetType().Name, exception);
      return null;
    }
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

    string trimmed = apiKey.Trim();
    ProtectedSecretWrite write;
    try
    {
      write = await Store.SetAsync(
        SiteXaiApiKeyNames.Name,
        SiteXaiApiKeyNames.Purpose,
        trimmed,
        setByPrincipalId,
        DateTimeOffset.UtcNow,
        cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (IsStorageFailure(exception))
    {
      LogStorageFailed(Logger, exception.GetType().Name, exception);
      return XaiApiKeyProblems.StorageFailed();
    }

    LogChanged(Logger, write == ProtectedSecretWrite.Inserted ? "set" : "replace", setByPrincipalId, null);
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

    try
    {
      _ = await Store.ClearAsync(SiteXaiApiKeyNames.Name, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (IsStorageFailure(exception))
    {
      LogStorageFailed(Logger, exception.GetType().Name, exception);
      return XaiApiKeyProblems.StorageFailed();
    }

    LogChanged(Logger, "clear", setByPrincipalId, null);
    return await GetStatusAsync(cancellationToken).ConfigureAwait(false);
  }

  private SiteXaiApiKeyStatus IdleStatus(bool host) =>
    new(
      ManagedByHost: host,
      Configured: host,
      StorageAvailable: Store.StorageAvailable,
      CanChange: Store.StorageAvailable && !host,
      NeedsReplacement: false,
      Suffix: null,
      SetAt: null,
      SetByPrincipalId: null);

  private bool HostKeyIsSet() => !string.IsNullOrWhiteSpace(Options.CurrentValue.ApiKey);

  private static bool IsStorageFailure(Exception exception)
  {
    for (Exception? current = exception; current is not null; current = current.InnerException)
    {
      if (current is OperationCanceledException)
      {
        return false;
      }

      if (current is TimeoutException or IOException)
      {
        return true;
      }

      string text = current.GetType().FullName + " " + current.Message;
      if (text.Contains("42P01", StringComparison.Ordinal)
        || text.Contains("Npgsql", StringComparison.Ordinal)
        || text.Contains("DbUpdateException", StringComparison.Ordinal)
        || text.Contains("DbException", StringComparison.Ordinal))
      {
        return true;
      }
    }

    return false;
  }
}
