#region Purpose
// EF IProtectedSecretStore: protects with the host Data Protection provider and stores ciphertext.
#endregion

#region Design
// Registered only from PostgresDbModule, which is also the only place that calls
// PersistKeysToDbContext. The purpose argument is the Data Protection purpose, stored on the row
// and checked again before unprotect. A purpose mismatch or a CryptographicException returns null
// and logs the failure type, not the payload. Set returns Inserted or Replaced. Two first writes
// that both Add hit the name primary key; the loser detaches the added row and replaces the
// winner. Last write wins. Task 399 passes a different name and purpose through the same methods.
#endregion

namespace TimeWarp.Architecture.Secrets;

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Persistence;

/// <summary>Postgres-backed <see cref="IProtectedSecretStore"/>.</summary>
public sealed class EfProtectedSecretStore : IProtectedSecretStore
{
  private static readonly Action<ILogger, string, Exception?> LogUnprotectFailed =
    LoggerMessage.Define<string>
    (
      LogLevel.Warning,
      new EventId(1, nameof(LogUnprotectFailed)),
      "Protected secret could not be unprotected ({Reason})"
    );

  private readonly PostgresDbContext Db;
  private readonly IDataProtectionProvider Protector;
  private readonly ILogger<EfProtectedSecretStore> Logger;

  /// <summary>Creates the store.</summary>
  public EfProtectedSecretStore(
    PostgresDbContext db,
    IDataProtectionProvider protector,
    ILogger<EfProtectedSecretStore> logger)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
    Protector = protector ?? throw new ArgumentNullException(nameof(protector));
    Logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <inheritdoc />
  public bool StorageAvailable => true;

  /// <inheritdoc />
  public async Task<ProtectedSecretRecord?> FindAsync(string name, CancellationToken cancellationToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    cancellationToken.ThrowIfCancellationRequested();
    ProtectedSecret? row = await Db.ProtectedSecrets.AsNoTracking()
      .FirstOrDefaultAsync(secret => secret.Name == name, cancellationToken)
      .ConfigureAwait(false);
    return row is null ? null : ToRecord(row);
  }

  /// <inheritdoc />
  public async Task<ProtectedSecretWrite> SetAsync(
    string name,
    string purpose,
    string plaintext,
    Guid setByPrincipalId,
    DateTimeOffset setAt,
    CancellationToken cancellationToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
    ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
    cancellationToken.ThrowIfCancellationRequested();

    string ciphertext = Protector.CreateProtector(purpose).Protect(plaintext);
    string suffix = ProtectedSecretSuffix.From(plaintext);
    ProtectedSecret? row = await Db.ProtectedSecrets
      .FirstOrDefaultAsync(secret => secret.Name == name, cancellationToken)
      .ConfigureAwait(false);
    if (row is not null)
    {
      row.Replace(purpose, ciphertext, suffix, setAt, setByPrincipalId);
      await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
      return ProtectedSecretWrite.Replaced;
    }

    Db.ProtectedSecrets.Add(ProtectedSecret.Create(name, purpose, ciphertext, suffix, setAt, setByPrincipalId));
    try
    {
      await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
      return ProtectedSecretWrite.Inserted;
    }
    catch (DbUpdateException exception) when (IsUniqueViolation(exception))
    {
      DetachAdded();
      ProtectedSecret? winner = await Db.ProtectedSecrets
        .FirstOrDefaultAsync(secret => secret.Name == name, cancellationToken)
        .ConfigureAwait(false);
      if (winner is null)
      {
        throw;
      }

      winner.Replace(purpose, ciphertext, suffix, setAt, setByPrincipalId);
      await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
      return ProtectedSecretWrite.Replaced;
    }
  }

  /// <inheritdoc />
  public async Task<string?> UnprotectAsync(string name, string purpose, CancellationToken cancellationToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
    cancellationToken.ThrowIfCancellationRequested();
    ProtectedSecret? row = await Db.ProtectedSecrets.AsNoTracking()
      .FirstOrDefaultAsync(secret => secret.Name == name, cancellationToken)
      .ConfigureAwait(false);
    if (row is null || !string.Equals(row.Purpose, purpose, StringComparison.Ordinal))
    {
      return null;
    }

    try
    {
      return Protector.CreateProtector(purpose).Unprotect(row.Ciphertext);
    }
    catch (CryptographicException exception)
    {
      LogUnprotectFailed(Logger, exception.GetType().Name, null);
      return null;
    }
  }

  /// <inheritdoc />
  public async Task<bool> ClearAsync(string name, CancellationToken cancellationToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    cancellationToken.ThrowIfCancellationRequested();
    ProtectedSecret? row = await Db.ProtectedSecrets
      .FirstOrDefaultAsync(secret => secret.Name == name, cancellationToken)
      .ConfigureAwait(false);
    if (row is null)
    {
      return false;
    }

    Db.ProtectedSecrets.Remove(row);
    await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return true;
  }

  private void DetachAdded()
  {
    foreach (EntityEntry<ProtectedSecret> entry in Db.ChangeTracker.Entries<ProtectedSecret>().ToList())
    {
      if (entry.State == EntityState.Added)
      {
        entry.State = EntityState.Detached;
      }
    }
  }

  private static bool IsUniqueViolation(DbUpdateException exception)
  {
    Exception? current = exception;
    while (current is not null)
    {
      string text = current.GetType().FullName + " " + current.Message;
      if (text.Contains("23505", StringComparison.Ordinal)
        || text.Contains("unique", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }

      current = current.InnerException;
    }

    return false;
  }

  private static ProtectedSecretRecord ToRecord(ProtectedSecret row) =>
    new(row.Name, row.Purpose, row.Suffix, row.SetAt, row.SetByPrincipalId);
}
