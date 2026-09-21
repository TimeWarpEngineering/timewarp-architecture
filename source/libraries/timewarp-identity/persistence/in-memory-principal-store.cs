#region Purpose
// Thread-safe in-memory IPrincipalStore for unit tests and early hosts before EF is wired.
#endregion

#region Design
// No EF in this package: ConcurrentDictionary plus foundation-domain's Entity<TId>/EntityVersion are
// the only dependencies — no third-party package. Uniqueness is principal id and (CredentialType,
// handle content). Multi-credential per principal is allowed. Host EF lives in web-infrastructure
// (EfPrincipalStore, task 104-032) and must match this store's CAS / Snapshot semantics exactly.
//
// Snapshot-on-get (task 104-028, supersedes RFC D4 → A): Get*/Find*/List*/Add* all return or store
// `entity.Snapshot(version)` — a fresh copy, never the caller's or the store's own instance. This
// replaced the original shared-reference model (every Get returned the SAME instance the store
// held, so concurrent field mutations were invisible races masked by nothing ever comparing
// versions). Snapshot-on-get is what makes the version check below meaningful: with shared
// references, two callers holding "the same object" can never legitimately disagree about its
// Version, so nothing could ever conflict. Every entity that leaves or enters the store through a
// public method is now a distinct instance from every other copy in flight.
//
// Version check (supersedes RFC D6 last-write-wins): Update* compares the caller's incoming Version
// against the stored row's Version. Mismatch throws ConcurrencyConflictException and leaves stored
// state untouched; match persists `incoming.Snapshot(EntityVersion.Next(stored.Version))`. Unknown
// id remains InvalidOperationException — absence and staleness are distinct failure classes. Full
// port contract text lives on IPrincipalStore's Design region.
//
// WriteLock, and why ConcurrentDictionary.TryUpdate cannot serve as the version CAS: TryUpdate's
// comparand match is Equals-based, and Entity<TId>'s equality (task 106) is exact-type + Id — it
// does NOT compare Version. Every same-Id snapshot of the same entity type is "equal" to every
// other one regardless of version, so `Principals.TryUpdate(id, newSnapshot, staleComparand)` would
// ALWAYS succeed (the comparand always "matches"), silently defeating the very check it exists to
// perform. A real mutual-exclusion lock around read-check-write is required instead; WriteLock
// (System.Threading.Lock) guards every mutation path (Add*/Update*) so the version compare-and-swap
// is atomic. Reads (Get*/Find*/List*) stay lock-free — ConcurrentDictionary's own reads are safe,
// and every value handed out is a snapshot the caller owns exclusively, so there is nothing for a
// concurrent write to corrupt from a reader's perspective.
//
// Type/Handle/PrincipalId immutable on Update (D7): UpdateCredentialAsync replaces by CredentialId
// only; differing type/handle/PrincipalId throws. Re-parent is MergePrincipalAsync only. Update
// purpose: revoke / restore (and future label) persistence. Check order inside
// UpdateCredentialAsync is deliberate: existence, THEN version (staleness dominates — a caller
// holding a stale-but-otherwise-valid credential should learn it is stale before learning about an
// unrelated type/handle/PrincipalId mismatch, since staleness is the more common,
// expected-to-be-retried case), THEN the type/handle/PrincipalId immutability check.
// Type/Handle immutability and Credentials.TryAdd-fails rollback are defensive (no public Type/Handle
// mutators; CredentialId minted only in Create) — no Type/Handle Update test without reflection.
// PrincipalId CAN change via ReparentTo, so that Update guard is caller-observable and contract-
// tested; re-parent only via MergePrincipalAsync. A duplicate CredentialId on Add implies a
// duplicate handle too (short of a Guid v7 collision), which HandleIndex.TryAdd catches first.
// FindCredentialByHandle returns the stored row even if revoked — callers check IsRevoked.
// MergePrincipalAsync runs under WriteLock: re-parent active credentials, raise target trust,
// copy DisplayName only when target's is empty, then source.MergeInto. Revoked credentials stay
// on the source. Each touched row is replaced with Snapshot(Next) so a stale Update* after merge
// conflicts.
//
// First credential: after successful AddCredentialAsync, the store constructs a NEW principal
// snapshot at EntityVersion.Next(stored.Version) and calls RecordCredentialAttached on THAT
// snapshot (never on the stored instance in place — nothing in this store ever mutates a
// dictionary-resident instance after it is stored). The store swaps it in ONLY when the tier
// actually changed (Provisional → Keyed); the already-Keyed-or-higher case leaves the stored
// principal (and its Version) untouched. This guard matters: bumping Version on every credential
// add — including the no-op case — would manufacture a spurious conflict for any concurrent
// principal writer who never touched the tier at all. A concurrent principal writer who DOES hold
// the pre-attach snapshot still conflicts correctly (their Version no longer matches once the tier
// changes), which is the intended "silently demoting the tier" prevention.
//
// Clocks (D5, closed 104-006): this store does not inject TimeProvider — it persists entity stamps
// as written; ceremony challenge/token stores already take optional TimeProvider. Domain CreatedAt/
// RevokedAt remain wall-clock with fuzzy tests; full TimeProvider on domain is not required for Wave 1.
// D8 material type remains byte[] copy-on-get on Credential (Snapshot's ToArray copies plus the
// getters' own ToArray copies mean a caller can never reach stored byte[] storage through any
// public surface).
#endregion

namespace TimeWarp.Identity;

using System.Collections.Concurrent;

/// <summary>
/// Process-local <see cref="IPrincipalStore"/> for tests and hosts before EF is wired.
/// </summary>
public sealed class InMemoryPrincipalStore : IPrincipalStore
{
  private readonly ConcurrentDictionary<PrincipalId, Principal> Principals = new();
  private readonly ConcurrentDictionary<CredentialId, Credential> Credentials = new();
  private readonly ConcurrentDictionary<HandleKey, CredentialId> HandleIndex = new();
  private readonly Lock WriteLock = new();

  /// <inheritdoc />
  public Task AddPrincipalAsync(Principal principal, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(principal);
    cancellationToken.ThrowIfCancellationRequested();

    lock (WriteLock)
    {
      if (!Principals.TryAdd(principal.Id, principal.Snapshot(principal.Version)))
      {
        throw new InvalidOperationException($"Principal '{principal.Id}' already exists.");
      }
    }

    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task<Principal?> GetPrincipalAsync(PrincipalId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    Principals.TryGetValue(id, out Principal? stored);
    return Task.FromResult(stored?.Snapshot(stored.Version));
  }

  /// <inheritdoc />
  public Task UpdatePrincipalAsync(Principal principal, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(principal);
    cancellationToken.ThrowIfCancellationRequested();

    lock (WriteLock)
    {
      if (!Principals.TryGetValue(principal.Id, out Principal? stored))
      {
        throw new InvalidOperationException($"Principal '{principal.Id}' does not exist.");
      }

      if (principal.Version != stored.Version)
      {
        throw new ConcurrencyConflictException(typeof(Principal), principal.Id.ToString(), principal.Version, stored.Version);
      }

      Principals[principal.Id] = principal.Snapshot(EntityVersion.Next(stored.Version));
    }

    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task<IReadOnlyList<Principal>> ListPrincipalsAsync(CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    IReadOnlyList<Principal> list =
      Principals.Values
        .OrderBy(p => p.CreatedAt)
        .Select(p => p.Snapshot(p.Version))
        .ToArray();

    return Task.FromResult(list);
  }

  /// <inheritdoc />
  public Task AddCredentialAsync(Credential credential, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(credential);
    cancellationToken.ThrowIfCancellationRequested();

    lock (WriteLock)
    {
      if (!Principals.TryGetValue(credential.PrincipalId, out Principal? storedPrincipal))
      {
        throw new InvalidOperationException($"Principal '{credential.PrincipalId}' does not exist.");
      }

      var handleKey = HandleKey.From(credential.Type, credential.Handle);
      if (!HandleIndex.TryAdd(handleKey, credential.Id))
      {
        throw new InvalidOperationException($"A credential with type '{credential.Type}' and the same handle already exists.");
      }

      if (!Credentials.TryAdd(credential.Id, credential.Snapshot(credential.Version)))
      {
        HandleIndex.TryRemove(handleKey, out _);
        throw new InvalidOperationException($"Credential '{credential.Id}' already exists.");
      }

      // First-credential birth rule: Provisional → Keyed (even if quarantined) — see Design region
      // for why the Version bump is conditional on the tier actually changing.
      Principal candidate = storedPrincipal.Snapshot(EntityVersion.Next(storedPrincipal.Version));
      candidate.RecordCredentialAttached();
      if (candidate.TrustTier != storedPrincipal.TrustTier)
      {
        Principals[credential.PrincipalId] = candidate;
      }
    }

    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task<Credential?> GetCredentialAsync(CredentialId credentialId, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    Credentials.TryGetValue(credentialId, out Credential? stored);
    return Task.FromResult(stored?.Snapshot(stored.Version));
  }

  /// <inheritdoc />
  public Task<Credential?> FindCredentialByHandleAsync(CredentialType type, byte[] handle, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(handle);
    cancellationToken.ThrowIfCancellationRequested();

    var handleKey = HandleKey.From(type, handle);
    if (!HandleIndex.TryGetValue(handleKey, out CredentialId credentialId))
    {
      return Task.FromResult<Credential?>(null);
    }

    Credentials.TryGetValue(credentialId, out Credential? stored);
    return Task.FromResult(stored?.Snapshot(stored.Version));
  }

  /// <inheritdoc />
  public Task<IReadOnlyList<Credential>> ListCredentialsAsync(
    PrincipalId principalId,
    bool includeRevoked = false,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    IReadOnlyList<Credential> list =
      Credentials.Values
        .Where(c => c.PrincipalId.Equals(principalId) && (includeRevoked || !c.IsRevoked))
        .OrderBy(c => c.CreatedAt)
        .Select(c => c.Snapshot(c.Version))
        .ToArray();

    return Task.FromResult(list);
  }

  /// <inheritdoc />
  public Task UpdateCredentialAsync(Credential credential, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(credential);
    cancellationToken.ThrowIfCancellationRequested();

    lock (WriteLock)
    {
      if (!Credentials.TryGetValue(credential.Id, out Credential? existing))
      {
        throw new InvalidOperationException($"Credential '{credential.Id}' does not exist.");
      }

      if (credential.Version != existing.Version)
      {
        throw new ConcurrencyConflictException(typeof(Credential), credential.Id.ToString(), credential.Version, existing.Version);
      }

      // Type and Handle are immutable — Update is for revoke (and similar) persistence by Id only (RFC D7).
      var existingKey = HandleKey.From(existing.Type, existing.Handle);
      var incomingKey = HandleKey.From(credential.Type, credential.Handle);
      if (!existingKey.Equals(incomingKey)
          || existing.PrincipalId != credential.PrincipalId)
      {
        throw new InvalidOperationException(
          existing.PrincipalId != credential.PrincipalId
            ? "PrincipalId is immutable on Update; re-parent only via MergePrincipalAsync."
            : "Credential Type and Handle are immutable; UpdateCredentialAsync cannot change them.");
      }

      Credentials[credential.Id] = credential.Snapshot(EntityVersion.Next(existing.Version));
    }

    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task MergePrincipalAsync(
    PrincipalId sourceId,
    PrincipalId targetId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    if (sourceId.IsEmpty)
    {
      throw new ArgumentException("Source PrincipalId cannot be empty.", nameof(sourceId));
    }

    if (targetId.IsEmpty)
    {
      throw new ArgumentException("Target PrincipalId cannot be empty.", nameof(targetId));
    }

    if (sourceId == targetId)
    {
      throw new ArgumentException("Source and target principals must differ.", nameof(targetId));
    }

    lock (WriteLock)
    {
      if (!Principals.TryGetValue(sourceId, out Principal? storedSource))
      {
        throw new InvalidOperationException($"Principal '{sourceId}' does not exist.");
      }

      if (!Principals.TryGetValue(targetId, out Principal? storedTarget))
      {
        throw new InvalidOperationException($"Principal '{targetId}' does not exist.");
      }

      Principal source = storedSource.Snapshot(storedSource.Version);
      Principal target = storedTarget.Snapshot(storedTarget.Version);
      if (source.MergedIntoPrincipalId is not null)
      {
        throw new InvalidOperationException($"Principal '{sourceId}' is already merged.");
      }

      if (!target.IsActive)
      {
        throw new InvalidOperationException($"Target principal '{targetId}' is not active.");
      }

      Credential[] activeSourceCredentials =
        Credentials.Values
          .Where(credential => credential.PrincipalId.Equals(sourceId) && !credential.IsRevoked)
          .ToArray();

      foreach (Credential storedCredential in activeSourceCredentials)
      {
        Credential moving = storedCredential.Snapshot(storedCredential.Version);
        moving.ReparentTo(targetId);
        Credentials[moving.Id] = moving.Snapshot(EntityVersion.Next(storedCredential.Version));
      }

      if (string.IsNullOrWhiteSpace(target.DisplayName) && !string.IsNullOrWhiteSpace(source.DisplayName))
      {
        target.SetDisplayName(source.DisplayName);
      }

      target.ApplyTrustAtLeast(source.TrustTier);
      source.MergeInto(targetId);

      Principals[sourceId] = source.Snapshot(EntityVersion.Next(storedSource.Version));
      Principals[targetId] = target.Snapshot(EntityVersion.Next(storedTarget.Version));
    }

    return Task.CompletedTask;
  }

  private readonly record struct HandleKey(CredentialType Type, string HandleHex)
  {
    public static HandleKey From(CredentialType type, byte[] handle) =>
      new(type, Convert.ToHexString(handle));
  }
}
