#region Purpose
// IProtectedSecretStore for hosts that have no durable Data Protection ring.
#endregion

#region Design
// Registered unless PostgresDbModule replaces it. Postgres off, or postgres on with no connection
// string, keeps this type. Set fails closed so a caller cannot believe a secret was saved on the
// file-system ring, which a new container would not share. The exception names the operation, not
// the plaintext. Find, Unprotect, and Clear are empty results.
#endregion

namespace TimeWarp.Architecture.Secrets;

/// <summary>No durable secret storage. Host configuration is the only place a secret can live.</summary>
public sealed class UnavailableProtectedSecretStore : IProtectedSecretStore
{
  /// <inheritdoc />
  public bool StorageAvailable => false;

  /// <inheritdoc />
  public Task<ProtectedSecretRecord?> FindAsync(string name, CancellationToken cancellationToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult<ProtectedSecretRecord?>(null);
  }

  /// <inheritdoc />
  public Task<ProtectedSecretWrite> SetAsync(
    string name,
    string purpose,
    string plaintext,
    Guid setByPrincipalId,
    DateTimeOffset setAt,
    CancellationToken cancellationToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
    ArgumentNullException.ThrowIfNull(plaintext);
    _ = setByPrincipalId;
    _ = setAt;
    cancellationToken.ThrowIfCancellationRequested();
    throw new InvalidOperationException("Protected secret storage is not available on this host.");
  }

  /// <inheritdoc />
  public Task<string?> UnprotectAsync(string name, string purpose, CancellationToken cancellationToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult<string?>(null);
  }

  /// <inheritdoc />
  public Task<bool> ClearAsync(string name, CancellationToken cancellationToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(false);
  }
}
