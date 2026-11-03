#region Purpose
// Shared IPrincipalStore contract case group Merge (task 104-032 dual-fixture); inherited by the
// in-memory and EF concrete fixtures so every backend pins the same semantics.
#endregion

namespace PrincipalStoreContract_;

using TimeWarp.Identity;

public abstract class Merge
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Moves_active_credentials_and_retires_source()
  {
    if (ShouldSkip()) return;
    IPrincipalStore store = Factory.CreateStore();
    Principal source = Principal.Create(PrincipalKind.Human);
    source.SetDisplayName("Source");
    Principal target = Principal.Create(PrincipalKind.Human);
    target.SetDisplayName("Target");
    await store.AddPrincipalAsync(source);
    await store.AddPrincipalAsync(target);

    Credential passkey = Credential.Create(source.Id, CredentialType.Passkey, [1], [2], "pk");
    Credential entra = Credential.Create(source.Id, CredentialType.EntraAccount, [3], [4], "m365");
    Credential revoked = Credential.Create(source.Id, CredentialType.AgentKey, [5], [6], "old");
    await store.AddCredentialAsync(passkey);
    await store.AddCredentialAsync(entra);
    await store.AddCredentialAsync(revoked);
    Credential? loadedRevoked = await store.GetCredentialAsync(revoked.Id);
    loadedRevoked.ShouldNotBeNull();
    loadedRevoked.Revoke();
    await store.UpdateCredentialAsync(loadedRevoked);

    await store.MergePrincipalAsync(source.Id, target.Id);

    Credential? movedPasskey = await store.GetCredentialAsync(passkey.Id);
    movedPasskey.ShouldNotBeNull();
    movedPasskey.PrincipalId.ShouldBe(target.Id);
    movedPasskey.IsRevoked.ShouldBeFalse();

    Credential? movedEntra = await store.GetCredentialAsync(entra.Id);
    movedEntra.ShouldNotBeNull();
    movedEntra.PrincipalId.ShouldBe(target.Id);

    Credential? stayedRevoked = await store.GetCredentialAsync(revoked.Id);
    stayedRevoked.ShouldNotBeNull();
    stayedRevoked.PrincipalId.ShouldBe(source.Id);
    stayedRevoked.IsRevoked.ShouldBeTrue();

    Principal? retired = await store.GetPrincipalAsync(source.Id);
    retired.ShouldNotBeNull();
    retired.IsActive.ShouldBeFalse();
    retired.MergedIntoPrincipalId.ShouldBe(target.Id);
    retired.Version.ShouldBeGreaterThan(0);

    Principal? surviving = await store.GetPrincipalAsync(target.Id);
    surviving.ShouldNotBeNull();
    surviving.IsActive.ShouldBeTrue();
    surviving.DisplayName.ShouldBe("Target");
    (await store.ListCredentialsAsync(target.Id)).Count.ShouldBe(2);
  }

  public async Task Target_trust_is_max_and_empty_display_name_is_copied()
  {
    if (ShouldSkip()) return;
    IPrincipalStore store = Factory.CreateStore();
    Principal source = Principal.Create(PrincipalKind.Human);
    source.SetDisplayName("Ada");
    Principal target = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(source);
    await store.AddPrincipalAsync(target);
    await store.AddCredentialAsync(Credential.Create(source.Id, CredentialType.Passkey, [1], [2]));
    Principal? loadedSource = await store.GetPrincipalAsync(source.Id);
    loadedSource.ShouldNotBeNull();
    loadedSource.Promote(TrustTier.Established);
    await store.UpdatePrincipalAsync(loadedSource);

    await store.MergePrincipalAsync(source.Id, target.Id);

    Principal? surviving = await store.GetPrincipalAsync(target.Id);
    surviving.ShouldNotBeNull();
    surviving.TrustTier.ShouldBe(TrustTier.Established);
    surviving.DisplayName.ShouldBe("Ada");
  }

  public async Task Second_merge_of_same_source_fails()
  {
    if (ShouldSkip()) return;
    IPrincipalStore store = Factory.CreateStore();
    Principal source = Principal.Create(PrincipalKind.Human);
    Principal first = Principal.Create(PrincipalKind.Human);
    Principal second = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(source);
    await store.AddPrincipalAsync(first);
    await store.AddPrincipalAsync(second);
    await store.AddCredentialAsync(Credential.Create(source.Id, CredentialType.Passkey, [1], [2]));

    await store.MergePrincipalAsync(source.Id, first.Id);
    await Should.ThrowAsync<InvalidOperationException>(() => store.MergePrincipalAsync(source.Id, second.Id));
  }

  public async Task Stale_source_update_after_merge_conflicts()
  {
    if (ShouldSkip()) return;
    IPrincipalStore store = Factory.CreateStore();
    Principal source = Principal.Create(PrincipalKind.Human);
    Principal target = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(source);
    await store.AddPrincipalAsync(target);
    await store.AddCredentialAsync(Credential.Create(source.Id, CredentialType.Passkey, [1], [2]));

    Principal? stale = await store.GetPrincipalAsync(source.Id);
    stale.ShouldNotBeNull();
    await store.MergePrincipalAsync(source.Id, target.Id);

    stale.SetDisplayName("stale");
    await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdatePrincipalAsync(stale));
  }

  public async Task Same_id_is_rejected()
  {
    if (ShouldSkip()) return;
    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);
    await Should.ThrowAsync<ArgumentException>(() => store.MergePrincipalAsync(principal.Id, principal.Id));
  }
}
