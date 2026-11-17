#region Purpose
// IPrincipalStore that loses the AddCredential race: first Find misses, AddCredential throws, re-Find returns the winner.
#endregion

namespace EntraTicketProcessor_;

using TimeWarp.Identity;

internal sealed class RacePrincipalStore : IPrincipalStore
{
  private readonly Principal WinnerPrincipal;
  private readonly Credential WinnerCredential;
  private int FindCredentialCallCount;

  public RacePrincipalStore(Principal winnerPrincipal, Credential winnerCredential)
  {
    WinnerPrincipal = winnerPrincipal;
    WinnerCredential = winnerCredential;
  }

  public int FindCredentialCalls => FindCredentialCallCount;
  public int AddPrincipalCalls { get; private set; }
  public int AddCredentialCalls { get; private set; }

  public Task AddPrincipalAsync(Principal principal, CancellationToken cancellationToken = default)
  {
    AddPrincipalCalls++;
    return Task.CompletedTask;
  }

  public Task<Principal?> GetPrincipalAsync(PrincipalId id, CancellationToken cancellationToken = default)
  {
    if (id == WinnerPrincipal.Id)
    {
      return Task.FromResult<Principal?>(WinnerPrincipal);
    }

    return Task.FromResult<Principal?>(null);
  }

  public Task UpdatePrincipalAsync(Principal principal, CancellationToken cancellationToken = default) =>
    Task.CompletedTask;

  public Task<IReadOnlyList<Principal>> ListPrincipalsAsync(CancellationToken cancellationToken = default) =>
    Task.FromResult<IReadOnlyList<Principal>>([WinnerPrincipal]);

  public Task AddCredentialAsync(Credential credential, CancellationToken cancellationToken = default)
  {
    AddCredentialCalls++;
    throw new InvalidOperationException(
      "A credential with type EntraAccount and this handle already exists.");
  }

  public Task<Credential?> GetCredentialAsync(
    CredentialId credentialId,
    CancellationToken cancellationToken = default) =>
    Task.FromResult<Credential?>(null);

  public Task<Credential?> FindCredentialByHandleAsync(
    CredentialType type,
    byte[] handle,
    CancellationToken cancellationToken = default)
  {
    FindCredentialCallCount++;
    if (FindCredentialCallCount == 1)
    {
      return Task.FromResult<Credential?>(null);
    }

    return Task.FromResult<Credential?>(WinnerCredential);
  }

  public Task<IReadOnlyList<Credential>> ListCredentialsAsync(
    PrincipalId principalId,
    bool includeRevoked = false,
    CancellationToken cancellationToken = default) =>
    Task.FromResult<IReadOnlyList<Credential>>([]);

  public Task UpdateCredentialAsync(Credential credential, CancellationToken cancellationToken = default) =>
    Task.CompletedTask;

  public Task MergePrincipalAsync(
    PrincipalId sourceId,
    PrincipalId targetId,
    CancellationToken cancellationToken = default) =>
    Task.CompletedTask;
}
