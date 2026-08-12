#region Purpose
// Handler-seam unit tests for task 104-005's 104-028 showcase: RevokeCredential.Handler's bounded
// catch-reGet-retry loop over ConcurrencyConflictException.
#endregion

#region Design
// Deliberately NOT an HTTP integration test (unlike the rest of this features/identity folder) — no
// WebTestServerApplication, no real host. A genuine HTTP-level concurrency conflict is
// non-deterministic (it depends on winning an actual race between two in-flight requests), so this
// drives the handler directly against a fake IPrincipalStore decorator that FORCES
// UpdateCredentialAsync to throw ConcurrencyConflictException a controlled number of times before
// delegating to a real InMemoryPrincipalStore — the only way to deterministically exercise "retry
// once then succeed" vs. "retry MaxAttempts times then give up" in a test.
// ThrowingUpdateCredentialStore wraps a real InMemoryPrincipalStore for every OTHER member (List/Get/
// Add all behave normally) and only intercepts UpdateCredentialAsync — this keeps the fake honest:
// the handler's own last-credential / already-revoked / not-found checks all still run against real
// store state, only the concurrency-conflict PATH is synthetic.
#endregion

namespace RevokeCredentialConcurrencyRetry_;

using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Identity;
// RevokeCredential exists in both TimeWarp.Architecture.Features.Identity (the contract) and
// TimeWarp.Architecture.Features.Identity.Application (the handler's own partial) — alias the
// Handler directly rather than `using` the Application namespace, which would make the bare name
// `RevokeCredential` ambiguous between the two.
using RevokeCredentialHandler = TimeWarp.Architecture.Features.Identity.Application.RevokeCredential.Handler;

public class Returns_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Returns_>();

  public static async Task Ok_After_One_Stale_Retry()
  {
    var innerStore = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await innerStore.AddPrincipalAsync(principal);

    Credential target = Credential.Create(principal.Id, CredentialType.Passkey, [1], [1]);
    Credential sibling = Credential.Create(principal.Id, CredentialType.Passkey, [2], [2]);
    await innerStore.AddCredentialAsync(target);
    await innerStore.AddCredentialAsync(sibling);

    var throwingStore = new ThrowingUpdateCredentialStore(innerStore, throwCount: 1);
    var handler = new RevokeCredentialHandler(throwingStore, new FixedPrincipalAccessor(principal.Id));

    OneOf<RevokeCredential.Response, SharedProblemDetails> result = await handler.Handle
    (
      new RevokeCredential.Command { CredentialId = target.Id.Value, UserId = Guid.NewGuid() },
      CancellationToken.None
    );

    result.IsT0.ShouldBeTrue("A single stale-version conflict should be absorbed by the retry loop, not surfaced to the caller.");
    // One throwing attempt + one successful attempt.
    throwingStore.UpdateCredentialAttempts.ShouldBe(2);

    Credential? reloaded = await innerStore.GetCredentialAsync(target.Id);
    reloaded.ShouldNotBeNull();
    reloaded.IsRevoked.ShouldBeTrue();
  }

  public static async Task TooMuchContention_After_MaxAttempts_Exhausted()
  {
    var innerStore = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await innerStore.AddPrincipalAsync(principal);

    Credential target = Credential.Create(principal.Id, CredentialType.Passkey, [1], [1]);
    Credential sibling = Credential.Create(principal.Id, CredentialType.Passkey, [2], [2]);
    await innerStore.AddCredentialAsync(target);
    await innerStore.AddCredentialAsync(sibling);

    // Always throws — the loop must give up after its bound, not retry forever.
    var throwingStore = new ThrowingUpdateCredentialStore(innerStore, throwCount: int.MaxValue);
    var handler = new RevokeCredentialHandler(throwingStore, new FixedPrincipalAccessor(principal.Id));

    OneOf<RevokeCredential.Response, SharedProblemDetails> result = await handler.Handle
    (
      new RevokeCredential.Command { CredentialId = target.Id.Value, UserId = Guid.NewGuid() },
      CancellationToken.None
    );

    result.IsT1.ShouldBeTrue("Sustained contention should surface as a rejection, not hang or silently succeed.");
    result.AsT1.Status.ShouldBe(409);
    // Mirrors RevokeCredential.Handler's private MaxAttempts=3 — the loop is bounded, not unbounded.
    throwingStore.UpdateCredentialAttempts.ShouldBe(3);

    Credential? reloaded = await innerStore.GetCredentialAsync(target.Id);
    reloaded.ShouldNotBeNull();
    reloaded.IsRevoked.ShouldBeFalse("Every attempt failed to persist — the credential must remain active in the store.");
  }

  private sealed class FixedPrincipalAccessor : ICurrentPrincipalAccessor
  {
    private readonly PrincipalId PrincipalId;

    public FixedPrincipalAccessor(PrincipalId principalId)
    {
      PrincipalId = principalId;
    }

    public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
      Task.FromResult<PrincipalId?>(PrincipalId);
  }

  private sealed class ThrowingUpdateCredentialStore : IPrincipalStore
  {
    private readonly IPrincipalStore Inner;
    private int ThrowCount;

    public ThrowingUpdateCredentialStore(IPrincipalStore inner, int throwCount)
    {
      Inner = inner;
      ThrowCount = throwCount;
    }

    public int UpdateCredentialAttempts { get; private set; }

    public Task AddPrincipalAsync(Principal principal, CancellationToken cancellationToken = default) =>
      Inner.AddPrincipalAsync(principal, cancellationToken);

    public Task<Principal?> GetPrincipalAsync(PrincipalId id, CancellationToken cancellationToken = default) =>
      Inner.GetPrincipalAsync(id, cancellationToken);

    public Task UpdatePrincipalAsync(Principal principal, CancellationToken cancellationToken = default) =>
      Inner.UpdatePrincipalAsync(principal, cancellationToken);

    public Task<IReadOnlyList<Principal>> ListPrincipalsAsync(CancellationToken cancellationToken = default) =>
      Inner.ListPrincipalsAsync(cancellationToken);

    public Task AddCredentialAsync(Credential credential, CancellationToken cancellationToken = default) =>
      Inner.AddCredentialAsync(credential, cancellationToken);

    public Task<Credential?> GetCredentialAsync(CredentialId credentialId, CancellationToken cancellationToken = default) =>
      Inner.GetCredentialAsync(credentialId, cancellationToken);

    public Task<Credential?> FindCredentialByHandleAsync(CredentialType type, byte[] handle, CancellationToken cancellationToken = default) =>
      Inner.FindCredentialByHandleAsync(type, handle, cancellationToken);

    public Task<IReadOnlyList<Credential>> ListCredentialsAsync(PrincipalId principalId, bool includeRevoked = false, CancellationToken cancellationToken = default) =>
      Inner.ListCredentialsAsync(principalId, includeRevoked, cancellationToken);

    public async Task UpdateCredentialAsync(Credential credential, CancellationToken cancellationToken = default)
    {
      UpdateCredentialAttempts++;

      if (ThrowCount > 0)
      {
        ThrowCount--;
        throw new ConcurrencyConflictException(typeof(Credential), credential.Id.ToString(), credential.Version, credential.Version + 1);
      }

      await Inner.UpdateCredentialAsync(credential, cancellationToken);
    }
  }
}
