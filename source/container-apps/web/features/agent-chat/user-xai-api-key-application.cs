#region Purpose
// Stores one user's xAI API key and removes it when that principal is retired.
#endregion

#region Design
// The row name and Data Protection purpose both include the user id. Set trims, checks the
// length bound, refuses when the site mode is Off, then asks IXaiApiKeyProbe. A probe problem
// is returned as-is and the row is left unchanged. A readable row's suffix comes from the store,
// which keeps the last four characters, so status does not unprotect just to render. Read
// unprotects with that user's purpose only. Delete removes the row and does not unprotect.
// Audit lines name the action and the user id. They do not include the key or the suffix.
// A store failure is 503 from status, set, and clear, and is logged as the exception type.
// The host key does not block a write: Ask ignores the row until the host key is cleared, and
// the status says so.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Secrets;
using TimeWarp.Identity;

/// <summary>One user's xAI API key on <see cref="IProtectedSecretStore"/>.</summary>
public interface IUserXaiApiKey
{
  /// <summary>Status for Profile. The plaintext is not returned.</summary>
  Task<OneOf<UserXaiApiKeyStatus, SharedProblemDetails>> GetStatusAsync(
    Guid userId,
    CancellationToken cancellationToken);

  /// <summary>Validates and stores a key, or a problem that does not contain it.</summary>
  Task<OneOf<UserXaiApiKeyStatus, SharedProblemDetails>> SetAsync(
    Guid userId,
    string apiKey,
    CancellationToken cancellationToken);

  /// <summary>Removes the user's key, or a problem when the mode is Off.</summary>
  Task<OneOf<UserXaiApiKeyStatus, SharedProblemDetails>> ClearAsync(
    Guid userId,
    CancellationToken cancellationToken);

  /// <summary>Removes the row for a retired principal. A missing row is success.</summary>
  Task DeleteForPrincipalAsync(Guid userId, CancellationToken cancellationToken);

  /// <summary>The plaintext for this user, or null when missing, unreadable, or the store throws.</summary>
  Task<string?> ReadAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>Per-user protected secret for the xAI API key.</summary>
public sealed class UserXaiApiKey : IUserXaiApiKey
{
  private static readonly Action<ILogger, string, Guid, Exception?> LogChanged =
    LoggerMessage.Define<string, Guid>
    (
      LogLevel.Information,
      new EventId(1, nameof(LogChanged)),
      "User xAI API key {Action} for {PrincipalId}"
    );

  private static readonly Action<ILogger, string, Exception?> LogStorageFailed =
    LoggerMessage.Define<string>
    (
      LogLevel.Warning,
      new EventId(2, nameof(LogStorageFailed)),
      "User xAI API key storage failed ({ExceptionType})"
    );

  private readonly IProtectedSecretStore Store;
  private readonly IXaiApiKeyProbe Probe;
  private readonly IOptionsMonitor<XaiChatOptions> Options;
  private readonly ISiteSettingsStore? Settings;
  private readonly IHostEnvironment Environment;
  private readonly ILogger<UserXaiApiKey> Logger;

  /// <summary>Creates the service. A missing settings store reads as Allow.</summary>
  public UserXaiApiKey(
    IProtectedSecretStore store,
    IXaiApiKeyProbe probe,
    IOptionsMonitor<XaiChatOptions> options,
    IHostEnvironment environment,
    ILogger<UserXaiApiKey> logger,
    ISiteSettingsStore? settings = null)
  {
    Store = store ?? throw new ArgumentNullException(nameof(store));
    Probe = probe ?? throw new ArgumentNullException(nameof(probe));
    Options = options ?? throw new ArgumentNullException(nameof(options));
    Environment = environment ?? throw new ArgumentNullException(nameof(environment));
    Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    Settings = settings;
  }

  /// <inheritdoc />
  public async Task<OneOf<UserXaiApiKeyStatus, SharedProblemDetails>> GetStatusAsync(
    Guid userId,
    CancellationToken cancellationToken)
  {
    XaiByokMode mode = await ReadModeAsync(cancellationToken).ConfigureAwait(false);
    bool host = HostKeyIsSet();
    if (mode == XaiByokMode.Off)
    {
      return Hidden(mode, host);
    }

    if (!Store.StorageAvailable)
    {
      return new UserXaiApiKeyStatus(
        visible: true,
        mode: mode,
        configured: false,
        needsReplacement: false,
        canChange: false,
        storageAvailable: false,
        supersededByHost: host,
        suffix: null);
    }

    try
    {
      ProtectedSecretRecord? row = await Store.FindAsync(UserXaiApiKeyNames.NameFor(userId), cancellationToken)
        .ConfigureAwait(false);
      string? plain = row is null
        ? null
        : await ReadAsync(userId, cancellationToken).ConfigureAwait(false);
      bool readable = row is not null && !string.IsNullOrWhiteSpace(plain);
      return new UserXaiApiKeyStatus(
        visible: true,
        mode: mode,
        configured: readable,
        needsReplacement: row is not null && !readable,
        canChange: true,
        storageAvailable: true,
        supersededByHost: host,
        suffix: row?.Suffix);
    }
    catch (Exception exception) when (IsStorageFailure(exception))
    {
      LogStorageFailed(Logger, exception.GetType().Name, exception);
      return XaiApiKeyProblems.StorageFailed();
    }
  }

  /// <inheritdoc />
  public async Task<OneOf<UserXaiApiKeyStatus, SharedProblemDetails>> SetAsync(
    Guid userId,
    string apiKey,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(apiKey);
    XaiByokMode mode = await ReadModeAsync(cancellationToken).ConfigureAwait(false);
    if (mode == XaiByokMode.Off)
    {
      return UserXaiApiKeyProblems.ByokOff();
    }

    if (!Store.StorageAvailable)
    {
      return XaiApiKeyProblems.StorageUnavailable(Environment.IsDevelopment());
    }

    if (!SiteXaiApiKeyNames.IsAcceptable(apiKey))
    {
      return UserXaiApiKeyProblems.Rejected();
    }

    string trimmed = apiKey.Trim();
    OneOf<bool, SharedProblemDetails> probed = await Probe.ValidateAsync(trimmed, cancellationToken)
      .ConfigureAwait(false);
    if (probed.IsT1)
    {
      return probed.AsT1;
    }

    if (!probed.AsT0)
    {
      return UserXaiApiKeyProblems.ProviderUnreachable();
    }

    try
    {
      ProtectedSecretWrite write = await Store.SetAsync(
        UserXaiApiKeyNames.NameFor(userId),
        UserXaiApiKeyNames.PurposeFor(userId),
        trimmed,
        userId,
        DateTimeOffset.UtcNow,
        cancellationToken).ConfigureAwait(false);
      LogChanged(Logger, write == ProtectedSecretWrite.Inserted ? "set" : "replace", userId, null);
    }
    catch (Exception exception) when (IsStorageFailure(exception))
    {
      LogStorageFailed(Logger, exception.GetType().Name, exception);
      return XaiApiKeyProblems.StorageFailed();
    }

    return await GetStatusAsync(userId, cancellationToken).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task<OneOf<UserXaiApiKeyStatus, SharedProblemDetails>> ClearAsync(
    Guid userId,
    CancellationToken cancellationToken)
  {
    XaiByokMode mode = await ReadModeAsync(cancellationToken).ConfigureAwait(false);
    if (mode == XaiByokMode.Off)
    {
      return UserXaiApiKeyProblems.ByokOff();
    }

    if (!Store.StorageAvailable)
    {
      return XaiApiKeyProblems.StorageUnavailable(Environment.IsDevelopment());
    }

    try
    {
      _ = await Store.ClearAsync(UserXaiApiKeyNames.NameFor(userId), cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (IsStorageFailure(exception))
    {
      LogStorageFailed(Logger, exception.GetType().Name, exception);
      return XaiApiKeyProblems.StorageFailed();
    }

    LogChanged(Logger, "clear", userId, null);
    return await GetStatusAsync(userId, cancellationToken).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task DeleteForPrincipalAsync(Guid userId, CancellationToken cancellationToken)
  {
    if (!Store.StorageAvailable)
    {
      return;
    }

    try
    {
      _ = await Store.ClearAsync(UserXaiApiKeyNames.NameFor(userId), cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (IsStorageFailure(exception))
    {
      LogStorageFailed(Logger, exception.GetType().Name, exception);
      return;
    }

    LogChanged(Logger, "delete", userId, null);
  }

  /// <inheritdoc />
  public async Task<string?> ReadAsync(Guid userId, CancellationToken cancellationToken)
  {
    if (!Store.StorageAvailable)
    {
      return null;
    }

    try
    {
      return await Store.UnprotectAsync(
        UserXaiApiKeyNames.NameFor(userId),
        UserXaiApiKeyNames.PurposeFor(userId),
        cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (IsStorageFailure(exception))
    {
      LogStorageFailed(Logger, exception.GetType().Name, exception);
      return null;
    }
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
    catch (Exception exception) when (IsStorageFailure(exception))
    {
      LogStorageFailed(Logger, exception.GetType().Name, exception);
      return XaiByokMode.Allow;
    }
  }

  private UserXaiApiKeyStatus Hidden(XaiByokMode mode, bool host) =>
    new(
      visible: false,
      mode: mode,
      configured: false,
      needsReplacement: false,
      canChange: false,
      storageAvailable: Store.StorageAvailable,
      supersededByHost: host,
      suffix: null);

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
