#region Purpose
// Persistence port for principals and credentials — hosts supply EF or other backends; tests use the in-memory impl.
#endregion

#region Design
// Credential lookup by CredentialId (RFC D3), not raw Guid. Type and Handle are immutable after Create — UpdateCredential
// persists revoke/label (and similar) changes for the same Id only; no handle reindex contract.
// Concurrency: last-write-wins (D6) — no version/etag on the port in Wave 1.
// FindCredentialByHandle may return revoked credentials (callers check IsRevoked).
// D4: shared-reference vs snapshot-on-get is an implementation choice; Wave 1 in-memory keeps shared refs.
// D5: TimeProvider not part of this port (deferred to 104-006).
#endregion

namespace TimeWarp.Identity;

public interface IPrincipalStore
{
  Task AddPrincipalAsync(Principal principal, CancellationToken cancellationToken = default);
  Task<Principal?> GetPrincipalAsync(PrincipalId id, CancellationToken cancellationToken = default);
  Task UpdatePrincipalAsync(Principal principal, CancellationToken cancellationToken = default);

  Task AddCredentialAsync(Credential credential, CancellationToken cancellationToken = default);
  Task<Credential?> GetCredentialAsync(CredentialId credentialId, CancellationToken cancellationToken = default);
  Task<Credential?> FindCredentialByHandleAsync(CredentialType type, byte[] handle, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<Credential>> ListCredentialsAsync(PrincipalId principalId, bool includeRevoked = false, CancellationToken cancellationToken = default);
  Task UpdateCredentialAsync(Credential credential, CancellationToken cancellationToken = default);
}
