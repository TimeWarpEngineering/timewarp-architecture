#region Purpose
// Thread-safe in-memory IPrincipalStore for unit tests and early hosts before EF is wired.
#endregion

#region Design
// No EF in this package for 104-002: ConcurrentDictionary keeps the library dependency-free. Uniqueness is principal id
// and (CredentialType, handle content). Multi-credential per principal is allowed. Hosts may later add EF without changing
// the IPrincipalStore surface.
//
// Reference semantics (RFC D4 → A): Get* returns the same entity instances held by the store (test double / early host).
// Concurrent field mutations on those entities are not synchronized — treat as single-threaded host or call Update* after
// mutate. Snapshot-on-get deferred (EF change-tracking hosts often mutate tracked entities without explicit Update).
//
// Last-write-wins (D6): Update* overwrites by id; no concurrency token in Wave 1.
// Type/Handle immutable (D7): UpdateCredentialAsync replaces by CredentialId only; differing type/handle throws rather
// than reindexing. Update purpose: revoke (and future label) persistence.
// FindCredentialByHandle returns the stored row even if revoked — callers check IsRevoked.
//
// First credential: after successful AddCredentialAsync, calls Principal.RecordCredentialAttached so Provisional → Keyed
// without requiring the host to remember the promotion rule.
//
// D5 TimeProvider deferred to 104-006. D8 material type remains byte[] copy-on-get on Credential.
#endregion

namespace TimeWarp.Identity;

using System.Collections.Concurrent;

public sealed class InMemoryPrincipalStore : IPrincipalStore
{
  private readonly ConcurrentDictionary<PrincipalId, Principal> Principals = new();
  private readonly ConcurrentDictionary<CredentialId, Credential> Credentials = new();
  private readonly ConcurrentDictionary<HandleKey, CredentialId> HandleIndex = new();

  public Task AddPrincipalAsync(Principal principal, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(principal);
    cancellationToken.ThrowIfCancellationRequested();

    if (!Principals.TryAdd(principal.Id, principal))
    {
      throw new InvalidOperationException($"Principal '{principal.Id}' already exists.");
    }

    return Task.CompletedTask;
  }

  public Task<Principal?> GetPrincipalAsync(PrincipalId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    Principals.TryGetValue(id, out Principal? principal);
    return Task.FromResult(principal);
  }

  public Task UpdatePrincipalAsync(Principal principal, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(principal);
    cancellationToken.ThrowIfCancellationRequested();

    if (!Principals.ContainsKey(principal.Id))
    {
      throw new InvalidOperationException($"Principal '{principal.Id}' does not exist.");
    }

    Principals[principal.Id] = principal;
    return Task.CompletedTask;
  }

  public Task AddCredentialAsync(Credential credential, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(credential);
    cancellationToken.ThrowIfCancellationRequested();

    if (!Principals.TryGetValue(credential.PrincipalId, out Principal? principal))
    {
      throw new InvalidOperationException($"Principal '{credential.PrincipalId}' does not exist.");
    }

    var handleKey = HandleKey.From(credential.Type, credential.Handle);
    if (!HandleIndex.TryAdd(handleKey, credential.Id))
    {
      throw new InvalidOperationException($"A credential with type '{credential.Type}' and the same handle already exists.");
    }

    if (!Credentials.TryAdd(credential.Id, credential))
    {
      HandleIndex.TryRemove(handleKey, out _);
      throw new InvalidOperationException($"Credential '{credential.Id}' already exists.");
    }

    // First-credential birth rule: Provisional → Keyed (even if quarantined); shared ref mutation intentional.
    principal.RecordCredentialAttached();

    return Task.CompletedTask;
  }

  public Task<Credential?> GetCredentialAsync(CredentialId credentialId, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    Credentials.TryGetValue(credentialId, out Credential? credential);
    return Task.FromResult(credential);
  }

  public Task<Credential?> FindCredentialByHandleAsync(CredentialType type, byte[] handle, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(handle);
    cancellationToken.ThrowIfCancellationRequested();

    var handleKey = HandleKey.From(type, handle);
    if (!HandleIndex.TryGetValue(handleKey, out CredentialId credentialId))
    {
      return Task.FromResult<Credential?>(null);
    }

    Credentials.TryGetValue(credentialId, out Credential? credential);
    return Task.FromResult(credential);
  }

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
        .ToArray();

    return Task.FromResult(list);
  }

  public Task UpdateCredentialAsync(Credential credential, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(credential);
    cancellationToken.ThrowIfCancellationRequested();

    if (!Credentials.TryGetValue(credential.Id, out Credential? existing))
    {
      throw new InvalidOperationException($"Credential '{credential.Id}' does not exist.");
    }

    // Type and Handle are immutable — Update is for revoke (and similar) persistence by Id only (RFC D7).
    var existingKey = HandleKey.From(existing.Type, existing.Handle);
    var incomingKey = HandleKey.From(credential.Type, credential.Handle);
    if (!existingKey.Equals(incomingKey))
    {
      throw new InvalidOperationException(
        "Credential Type and Handle are immutable; UpdateCredentialAsync cannot change them.");
    }

    Credentials[credential.Id] = credential;
    return Task.CompletedTask;
  }

  private readonly record struct HandleKey(CredentialType Type, string HandleHex)
  {
    public static HandleKey From(CredentialType type, byte[] handle) =>
      new(type, Convert.ToHexString(handle));
  }
}
