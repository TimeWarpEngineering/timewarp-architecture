#region Purpose
// Thread-safe in-memory IPrincipalStore for unit tests and early hosts before EF is wired.
#endregion

#region Design
// No EF in this package for 104-002: ConcurrentDictionary keeps the library dependency-free. Uniqueness is principal id
// and (CredentialType, handle content). Multi-credential per principal is allowed. Hosts may later add EF without changing
// the IPrincipalStore surface.
// Reference semantics: Get* returns the same entity instances held by the store (test double / early host). Concurrent
// field mutations on those entities are not synchronized — treat as single-threaded host or call Update* after mutate.
#endregion

namespace TimeWarp.Identity;

using System.Collections.Concurrent;

public sealed class InMemoryPrincipalStore : IPrincipalStore
{
  private readonly ConcurrentDictionary<PrincipalId, Principal> Principals = new();
  private readonly ConcurrentDictionary<Guid, Credential> Credentials = new();
  private readonly ConcurrentDictionary<HandleKey, Guid> HandleIndex = new();

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

    if (!Principals.ContainsKey(credential.PrincipalId))
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

    return Task.CompletedTask;
  }

  public Task<Credential?> GetCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default)
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
    if (!HandleIndex.TryGetValue(handleKey, out Guid credentialId))
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

    var existingKey = HandleKey.From(existing.Type, existing.Handle);
    var newKey = HandleKey.From(credential.Type, credential.Handle);

    if (!existingKey.Equals(newKey))
    {
      if (!HandleIndex.TryAdd(newKey, credential.Id))
      {
        throw new InvalidOperationException($"A credential with type '{credential.Type}' and the same handle already exists.");
      }

      HandleIndex.TryRemove(existingKey, out _);
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
