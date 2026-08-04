#region Purpose
// EF Core IPrincipalStore: durable Principal + Credential persistence behind the postgres flag.
#endregion

#region Design
// Host-owned (this assembly), not TimeWarp.Identity — the library stays EF-free and only ships
// the port + in-memory impl (task 104-032). Semantics MUST match InMemoryPrincipalStore exactly:
//   - Snapshot-on-get (every Get*/Find*/List* returns entity.Snapshot(version))
//   - Add* persists Version as-is
//   - Update* CAS: mismatch → ConcurrencyConflictException (store untouched); match → persist
//     Snapshot(EntityVersion.Next(stored)); caller's instance is not advanced
//   - AddCredential: Handle uniqueness; first credential Provisional→Keyed with conditional
//     principal Version bump only when the tier actually changes
//   - Type/Handle immutable on UpdateCredential
//   - List ordered by CreatedAt ascending
// Version authority is store-CAS, not AggregateDbContext: Principal/Credential are deliberately
// NOT IAggregateRoot, so SaveChanges will not auto-increment Version or run DomainInvariantsGuard.
// Mapping pairs .IsConcurrencyToken() as a second belt for true concurrent writers; a
// DbUpdateConcurrencyException is translated to ConcurrencyConflictException so callers see one
// conflict type regardless of backend.
// Scoped lifetime: depends on scoped PostgresDbContext. InMemoryIdentityStoresModule still registers
// singleton InMemoryPrincipalStore; PostgresDbModule replaces with this type when a connection
// string is present (skip-mode keeps in-memory).
// Reads use AsNoTracking so the change tracker never aliases a returned Snapshot. Writes load
// a tracked row for the CAS compare, then detach + attach the next Snapshot with
// OriginalValue(Version) = stored Version so the concurrency token WHERE clause is correct.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Infrastructure;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using TimeWarp.Architecture.Persistence;
using TimeWarp.Foundation.Entities;
using TimeWarp.Identity;

public sealed class EfPrincipalStore : IPrincipalStore
{
  private readonly PostgresDbContext Db;

  public EfPrincipalStore(PostgresDbContext db)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task AddPrincipalAsync(Principal principal, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(principal);
    cancellationToken.ThrowIfCancellationRequested();

    Principal stored = principal.Snapshot(principal.Version);
    bool exists = await Db.Principals.AsNoTracking()
      .AnyAsync(p => p.Id == principal.Id, cancellationToken)
      .ConfigureAwait(false);
    if (exists)
    {
      throw new InvalidOperationException($"Principal '{principal.Id}' already exists.");
    }

    Db.Principals.Add(stored);
    try
    {
      await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (DbUpdateException exception) when (IsUniqueViolation(exception))
    {
      Db.Entry(stored).State = EntityState.Detached;
      throw new InvalidOperationException($"Principal '{principal.Id}' already exists.", exception);
    }
  }

  public async Task<Principal?> GetPrincipalAsync(PrincipalId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    Principal? stored = await Db.Principals.AsNoTracking()
      .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
      .ConfigureAwait(false);
    return stored?.Snapshot(stored.Version);
  }

  public async Task<IReadOnlyList<Principal>> ListPrincipalsAsync(CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    List<Principal> rows = await Db.Principals.AsNoTracking()
      .OrderBy(p => p.CreatedAt)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return rows.Select(p => p.Snapshot(p.Version)).ToArray();
  }

  public async Task UpdatePrincipalAsync(Principal principal, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(principal);
    cancellationToken.ThrowIfCancellationRequested();

    Principal? stored = await Db.Principals
      .FirstOrDefaultAsync(p => p.Id == principal.Id, cancellationToken)
      .ConfigureAwait(false);
    if (stored is null)
    {
      throw new InvalidOperationException($"Principal '{principal.Id}' does not exist.");
    }

    if (principal.Version != stored.Version)
    {
      Db.Entry(stored).State = EntityState.Detached;
      throw new ConcurrencyConflictException(
        typeof(Principal),
        principal.Id.ToString(),
        principal.Version,
        stored.Version);
    }

    long storedVersion = stored.Version;
    Principal next = principal.Snapshot(EntityVersion.Next(storedVersion));
    Db.Entry(stored).State = EntityState.Detached;
    await PersistReplacementAsync(Db.Principals, next, storedVersion, cancellationToken).ConfigureAwait(false);
  }

  public async Task AddCredentialAsync(Credential credential, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(credential);
    cancellationToken.ThrowIfCancellationRequested();

    Principal? storedPrincipal = await Db.Principals
      .FirstOrDefaultAsync(p => p.Id == credential.PrincipalId, cancellationToken)
      .ConfigureAwait(false);
    if (storedPrincipal is null)
    {
      throw new InvalidOperationException($"Principal '{credential.PrincipalId}' does not exist.");
    }

    byte[] handle = credential.Handle;
    bool handleTaken = await Db.Credentials.AsNoTracking()
      .AnyAsync(
        c => c.Type == credential.Type && c.Handle == handle,
        cancellationToken)
      .ConfigureAwait(false);
    if (handleTaken)
    {
      Db.Entry(storedPrincipal).State = EntityState.Detached;
      throw new InvalidOperationException(
        $"A credential with type '{credential.Type}' and the same handle already exists.");
    }

    bool idExists = await Db.Credentials.AsNoTracking()
      .AnyAsync(c => c.Id == credential.Id, cancellationToken)
      .ConfigureAwait(false);
    if (idExists)
    {
      Db.Entry(storedPrincipal).State = EntityState.Detached;
      throw new InvalidOperationException($"Credential '{credential.Id}' already exists.");
    }

    Credential storedCredential = credential.Snapshot(credential.Version);
    Db.Credentials.Add(storedCredential);

    // First-credential birth rule: Provisional → Keyed (even if quarantined) — Version bump is
    // conditional on the tier actually changing (parity with InMemoryPrincipalStore).
    Principal candidate = storedPrincipal.Snapshot(EntityVersion.Next(storedPrincipal.Version));
    candidate.RecordCredentialAttached();
    bool tierChanged = candidate.TrustTier != storedPrincipal.TrustTier;
    long principalStoredVersion = storedPrincipal.Version;
    if (tierChanged)
    {
      Db.Entry(storedPrincipal).State = EntityState.Detached;
      AttachAsModified(Db.Principals, candidate, principalStoredVersion);
    }
    else
    {
      Db.Entry(storedPrincipal).State = EntityState.Detached;
    }

    try
    {
      await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (DbUpdateConcurrencyException exception)
    {
      DetachIfTracked(storedCredential);
      DetachIfTracked(candidate);
      throw TranslateConcurrency(exception, typeof(Principal), credential.PrincipalId.ToString(), principalStoredVersion);
    }
    catch (DbUpdateException exception) when (IsUniqueViolation(exception))
    {
      DetachIfTracked(storedCredential);
      DetachIfTracked(candidate);
      throw new InvalidOperationException(
        $"A credential with type '{credential.Type}' and the same handle already exists.",
        exception);
    }
  }

  public async Task<Credential?> GetCredentialAsync(CredentialId credentialId, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    Credential? stored = await Db.Credentials.AsNoTracking()
      .FirstOrDefaultAsync(c => c.Id == credentialId, cancellationToken)
      .ConfigureAwait(false);
    return stored?.Snapshot(stored.Version);
  }

  public async Task<Credential?> FindCredentialByHandleAsync(
    CredentialType type,
    byte[] handle,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(handle);
    cancellationToken.ThrowIfCancellationRequested();

    Credential? stored = await Db.Credentials.AsNoTracking()
      .FirstOrDefaultAsync(c => c.Type == type && c.Handle == handle, cancellationToken)
      .ConfigureAwait(false);
    return stored?.Snapshot(stored.Version);
  }

  public async Task<IReadOnlyList<Credential>> ListCredentialsAsync(
    PrincipalId principalId,
    bool includeRevoked = false,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    IQueryable<Credential> query = Db.Credentials.AsNoTracking()
      .Where(c => c.PrincipalId == principalId);
    if (!includeRevoked)
    {
      query = query.Where(c => c.RevokedAt == null);
    }

    List<Credential> rows = await query
      .OrderBy(c => c.CreatedAt)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return rows.Select(c => c.Snapshot(c.Version)).ToArray();
  }

  public async Task UpdateCredentialAsync(Credential credential, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(credential);
    cancellationToken.ThrowIfCancellationRequested();

    Credential? existing = await Db.Credentials
      .FirstOrDefaultAsync(c => c.Id == credential.Id, cancellationToken)
      .ConfigureAwait(false);
    if (existing is null)
    {
      throw new InvalidOperationException($"Credential '{credential.Id}' does not exist.");
    }

    if (credential.Version != existing.Version)
    {
      long actual = existing.Version;
      Db.Entry(existing).State = EntityState.Detached;
      throw new ConcurrencyConflictException(
        typeof(Credential),
        credential.Id.ToString(),
        credential.Version,
        actual);
    }

    // Type and Handle are immutable — Update is for revoke (and similar) persistence by Id only.
    if (existing.Type != credential.Type
        || !existing.Handle.AsSpan().SequenceEqual(credential.Handle))
    {
      Db.Entry(existing).State = EntityState.Detached;
      throw new InvalidOperationException(
        "Credential Type and Handle are immutable; UpdateCredentialAsync cannot change them.");
    }

    long storedVersion = existing.Version;
    Credential next = credential.Snapshot(EntityVersion.Next(storedVersion));
    Db.Entry(existing).State = EntityState.Detached;
    await PersistReplacementAsync(Db.Credentials, next, storedVersion, cancellationToken).ConfigureAwait(false);
  }

  private async Task PersistReplacementAsync<TEntity>(
    DbSet<TEntity> set,
    TEntity next,
    long originalVersion,
    CancellationToken cancellationToken)
    where TEntity : class
  {
    AttachAsModified(set, next, originalVersion);
    try
    {
      await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (DbUpdateConcurrencyException exception)
    {
      DetachIfTracked(next);
      string id = next switch
      {
        Principal principal => principal.Id.ToString(),
        Credential credential => credential.Id.ToString(),
        _ => "?"
      };
      Type entityType = next.GetType();
      throw TranslateConcurrency(exception, entityType, id, originalVersion);
    }
  }

  private void AttachAsModified<TEntity>(DbSet<TEntity> set, TEntity entity, long originalVersion)
    where TEntity : class
  {
    set.Attach(entity);
    EntityEntry<TEntity> entry = Db.Entry(entity);
    entry.State = EntityState.Modified;
    entry.Property("Version").OriginalValue = originalVersion;
  }

  private void DetachIfTracked(object entity)
  {
    EntityEntry entry = Db.Entry(entity);
    if (entry.State != EntityState.Detached)
    {
      entry.State = EntityState.Detached;
    }
  }

  private static ConcurrencyConflictException TranslateConcurrency(
    DbUpdateConcurrencyException exception,
    Type entityType,
    string entityId,
    long expectedVersion)
  {
    // True concurrent writer won the race after our pre-check. Actual version is unknown without
    // a reload; report expected+1 as a best-effort actual so callers still catch
    // ConcurrencyConflictException rather than a provider exception.
    _ = exception;
    return new ConcurrencyConflictException(
      entityType,
      entityId,
      expectedVersion,
      expectedVersion + 1);
  }

  private static bool IsUniqueViolation(DbUpdateException exception)
  {
    // Npgsql: 23505 unique_violation. Keep the check string-based so this file does not take a
    // hard dependency on Npgsql types beyond what EF already surfaces.
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
}
