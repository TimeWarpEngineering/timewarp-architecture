#region Purpose
// Named secret port: ciphertext stays in the store, callers see status or plaintext only.
#endregion

#region Design
// Task 398 stores the site xAI API key here. Task 399 stores each user's key as another row,
// named user.{id}.xai.api-key with a purpose that includes that id, not a second table. The record has no ciphertext and no
// plaintext: Find is the admin status, Unprotect is the server-side read. StorageAvailable is
// false when this process has no durable ring (postgres off, or no connection string). The
// unavailable implementation is that case. The EF implementation is registered only beside
// PersistKeysToDbContext. A suffix is the last four characters, kept so admin can say
// "ends in …xxxx" without unprotecting. The purpose string is stored with the row and must match
// on unprotect, so a row protected for one purpose cannot be opened with another.
#endregion

namespace TimeWarp.Architecture.Secrets;

/// <summary>Status of one named protected secret. No ciphertext and no plaintext.</summary>
public sealed record ProtectedSecretRecord(
  string Name,
  string Purpose,
  string Suffix,
  DateTimeOffset SetAt,
  Guid SetByPrincipalId);

/// <summary>Last four characters of a secret, for a write-only status line.</summary>
public static class ProtectedSecretSuffix
{
  /// <summary>How many trailing characters a status may show.</summary>
  public const int Length = 4;

  /// <summary>Trailing characters of <paramref name="plaintext"/> after trim. Short values are returned whole.</summary>
  public static string From(string plaintext)
  {
    ArgumentNullException.ThrowIfNull(plaintext);
    string trimmed = plaintext.Trim();
    return trimmed.Length <= Length ? trimmed : trimmed[^Length..];
  }
}

/// <summary>Whether <see cref="IProtectedSecretStore.SetAsync"/> inserted a row or replaced one.</summary>
public enum ProtectedSecretWrite
{
  /// <summary>No row existed. This call inserted it.</summary>
  Inserted,

  /// <summary>A row existed, including a concurrent insert this call then replaced.</summary>
  Replaced
}

/// <summary>Durable named secrets. The plaintext exists only for the duration of Set or Unprotect.</summary>
public interface IProtectedSecretStore
{
  /// <summary>False when this host cannot store a secret that survives a restart.</summary>
  bool StorageAvailable { get; }

  /// <summary>The row's status, or null when no secret is stored under <paramref name="name"/>.</summary>
  Task<ProtectedSecretRecord?> FindAsync(string name, CancellationToken cancellationToken);

  /// <summary>
  /// Protects <paramref name="plaintext"/> for <paramref name="purpose"/> and upserts the row.
  /// A concurrent first insert is retried as a replace.
  /// </summary>
  Task<ProtectedSecretWrite> SetAsync(
    string name,
    string purpose,
    string plaintext,
    Guid setByPrincipalId,
    DateTimeOffset setAt,
    CancellationToken cancellationToken);

  /// <summary>
  /// The plaintext when the row exists and was protected for <paramref name="purpose"/>.
  /// Null when missing, when the purpose does not match, or when unprotect fails.
  /// </summary>
  Task<string?> UnprotectAsync(string name, string purpose, CancellationToken cancellationToken);

  /// <summary>True when a row was removed.</summary>
  Task<bool> ClearAsync(string name, CancellationToken cancellationToken);
}
