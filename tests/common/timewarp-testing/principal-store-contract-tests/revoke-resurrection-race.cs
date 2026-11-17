#region Purpose
// Shared IPrincipalStore contract case group RevokeResurrectionRace (task 104-032 dual-fixture); inherited by the
// in-memory and EF concrete fixtures so every backend pins the same semantics.
#endregion

namespace PrincipalStoreContract_;

using TimeWarp.Identity;

public abstract class RevokeResurrectionRace
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Stale_update_after_revoke_throws_and_store_stays_revoked()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Credential credential = Credential.Create(principal.Id, CredentialType.Passkey, [1], [2], "laptop");
    await store.AddCredentialAsync(credential);

    Credential? a = await store.GetCredentialAsync(credential.Id);
    Credential? b = await store.GetCredentialAsync(credential.Id);
    a.ShouldNotBeNull();
    b.ShouldNotBeNull();

    a.Revoke();
    await store.UpdateCredentialAsync(a);

    await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdateCredentialAsync(b));

    Credential? reloaded = await store.GetCredentialAsync(credential.Id);
    reloaded.ShouldNotBeNull();
    reloaded.IsRevoked.ShouldBeTrue();
  }
}
