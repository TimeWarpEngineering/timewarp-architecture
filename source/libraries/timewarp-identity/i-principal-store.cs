#region Purpose
// Persistence port for principals and credentials — hosts supply EF or other backends; tests use the in-memory impl.
#endregion

namespace TimeWarp.Identity;

public interface IPrincipalStore
{
  Task AddPrincipalAsync(Principal principal, CancellationToken cancellationToken = default);
  Task<Principal?> GetPrincipalAsync(PrincipalId id, CancellationToken cancellationToken = default);
  Task UpdatePrincipalAsync(Principal principal, CancellationToken cancellationToken = default);

  Task AddCredentialAsync(Credential credential, CancellationToken cancellationToken = default);
  Task<Credential?> GetCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default);
  Task<Credential?> FindCredentialByHandleAsync(CredentialType type, byte[] handle, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<Credential>> ListCredentialsAsync(PrincipalId principalId, bool includeRevoked = false, CancellationToken cancellationToken = default);
  Task UpdateCredentialAsync(Credential credential, CancellationToken cancellationToken = default);
}
