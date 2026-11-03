#region Purpose
// Shared IPrincipalStore contract case group AttachBumpsPrincipalVersion (task 104-032 dual-fixture); inherited by the
// in-memory and EF concrete fixtures so every backend pins the same semantics.
#endregion

namespace PrincipalStoreContract_;

using TimeWarp.Identity;

public abstract class AttachBumpsPrincipalVersion
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Pre_attach_snapshot_update_conflicts_after_first_credential()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Principal? preAttach = await store.GetPrincipalAsync(principal.Id);
    preAttach.ShouldNotBeNull();

    Credential credential = Credential.Create(principal.Id, CredentialType.Passkey, [1], [2]);
    await store.AddCredentialAsync(credential);

    preAttach.SetDisplayName("stale");
    await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdatePrincipalAsync(preAttach));
  }

  public async Task Second_credential_add_does_not_bump_version_again()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    await store.AddCredentialAsync(Credential.Create(principal.Id, CredentialType.Passkey, [1], [2]));
    Principal? afterFirst = await store.GetPrincipalAsync(principal.Id);
    afterFirst.ShouldNotBeNull();
    afterFirst.Version.ShouldBe(1);
    afterFirst.TrustTier.ShouldBe(TrustTier.Keyed);

    await store.AddCredentialAsync(Credential.Create(principal.Id, CredentialType.AgentKey, [3], [4]));
    Principal? afterSecond = await store.GetPrincipalAsync(principal.Id);
    afterSecond.ShouldNotBeNull();
    afterSecond.Version.ShouldBe(1);
  }
}
