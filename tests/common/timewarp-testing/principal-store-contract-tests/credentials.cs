#region Purpose
// Shared IPrincipalStore contract case group Credentials (task 104-032 dual-fixture); inherited by the
// in-memory and EF concrete fixtures so every backend pins the same semantics.
#endregion

namespace PrincipalStoreContract_;

using TimeWarp.Identity;

public abstract class Credentials
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task First_credential_promotes_provisional_to_keyed()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);
    principal.TrustTier.ShouldBe(TrustTier.Provisional);

    Credential credential = Credential.Create(principal.Id, CredentialType.Passkey, [1], [2]);
    await store.AddCredentialAsync(credential);

    Principal? loaded = await store.GetPrincipalAsync(principal.Id);
    loaded.ShouldNotBeNull();
    loaded.TrustTier.ShouldBe(TrustTier.Keyed);
  }

  public async Task First_credential_promotes_to_keyed_even_when_quarantined()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.Quarantine();
    await store.AddPrincipalAsync(principal);

    await store.AddCredentialAsync(Credential.Create(principal.Id, CredentialType.Passkey, [1], [2]));

    Principal? loaded = await store.GetPrincipalAsync(principal.Id);
    loaded.ShouldNotBeNull();
    loaded.TrustTier.ShouldBe(TrustTier.Keyed);
    loaded.IsQuarantined.ShouldBeTrue();
    loaded.IsActive.ShouldBeFalse();
  }

  public async Task Multi_credential_per_principal_is_allowed()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Credential passkey = Credential.Create(principal.Id, CredentialType.Passkey, [1, 1], [2, 2], "passkey");
    Credential agentKey = Credential.Create(principal.Id, CredentialType.AgentKey, [3, 3], [4, 4], "agent");

    await store.AddCredentialAsync(passkey);
    await store.AddCredentialAsync(agentKey);

    IReadOnlyList<Credential> list = await store.ListCredentialsAsync(principal.Id);
    list.Count.ShouldBe(2);

    // Snapshot-on-get: caller's original principal is never mutated by AddCredentialAsync.
    Principal? loaded = await store.GetPrincipalAsync(principal.Id);
    loaded.ShouldNotBeNull();
    loaded.TrustTier.ShouldBe(TrustTier.Keyed);
  }

  public async Task Find_by_handle_returns_match()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Agent);
    await store.AddPrincipalAsync(principal);

    byte[] handle = [10, 20, 30];
    Credential credential = Credential.Create(principal.Id, CredentialType.AgentKey, handle, [40]);
    await store.AddCredentialAsync(credential);

    Credential? found = await store.FindCredentialByHandleAsync(CredentialType.AgentKey, [10, 20, 30]);
    found.ShouldNotBeNull();
    found.Id.ShouldBe(credential.Id);
  }

  public async Task Find_by_handle_returns_revoked_credential()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    byte[] handle = [11, 22];
    Credential credential = Credential.Create(principal.Id, CredentialType.Passkey, handle, [1]);
    await store.AddCredentialAsync(credential);
    credential.Revoke();
    await store.UpdateCredentialAsync(credential);

    Credential? found = await store.FindCredentialByHandleAsync(CredentialType.Passkey, handle);
    found.ShouldNotBeNull();
    found.IsRevoked.ShouldBeTrue();
  }

  public async Task Duplicate_type_and_handle_fails()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    byte[] handle = [7, 7];
    await store.AddCredentialAsync(Credential.Create(principal.Id, CredentialType.Passkey, handle, [1]));

    Credential duplicate = Credential.Create(principal.Id, CredentialType.Passkey, handle.ToArray(), [2]);
    await Should.ThrowAsync<InvalidOperationException>(() => store.AddCredentialAsync(duplicate));
  }

  public async Task Same_handle_different_type_is_allowed()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    byte[] handle = [8, 8];
    await store.AddCredentialAsync(Credential.Create(principal.Id, CredentialType.Passkey, handle, [1]));
    await store.AddCredentialAsync(Credential.Create(principal.Id, CredentialType.AgentKey, handle.ToArray(), [2]));

    (await store.ListCredentialsAsync(principal.Id)).Count.ShouldBe(2);
  }

  public async Task Missing_principal_fails_credential_add()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);
    await Should.ThrowAsync<InvalidOperationException>(() => store.AddCredentialAsync(credential));
  }

  public async Task List_excludes_revoked_by_default()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Credential active = Credential.Create(principal.Id, CredentialType.Passkey, [1], [2]);
    Credential revoked = Credential.Create(principal.Id, CredentialType.Passkey, [3], [4]);
    await store.AddCredentialAsync(active);
    await store.AddCredentialAsync(revoked);
    revoked.Revoke();
    await store.UpdateCredentialAsync(revoked);

    IReadOnlyList<Credential> activeOnly = await store.ListCredentialsAsync(principal.Id);
    activeOnly.Count.ShouldBe(1);
    activeOnly[0].Id.ShouldBe(active.Id);

    IReadOnlyList<Credential> all = await store.ListCredentialsAsync(principal.Id, includeRevoked: true);
    all.Count.ShouldBe(2);
  }

  public async Task Get_credential_by_id()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Service);
    await store.AddPrincipalAsync(principal);

    Credential credential = Credential.Create(principal.Id, CredentialType.AgentKey, [5], [6]);
    await store.AddCredentialAsync(credential);

    Credential? loaded = await store.GetCredentialAsync(credential.Id);
    loaded.ShouldNotBeNull();
    loaded.PrincipalId.ShouldBe(principal.Id);
    loaded.Id.ShouldBe(credential.Id);
  }

  /// <summary>
  /// 248-001 (review M5): nickname and the three RegisteredWith scalars survive Add → Get and a
  /// Rename → Update → Get cycle on every backend (EF binds them through the private constructor).
  /// </summary>
  public async Task Nickname_and_registered_with_round_trip_through_update()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    RegisteredWith registeredWith = new(AuthenticatorAttachment.Platform, browser: "Chrome", os: "Windows");
    Credential credential = Credential.Create(
      principal.Id,
      CredentialType.Passkey,
      [7, 7],
      [8, 8],
      label: "Proton Pass",
      nickname: "  work laptop ",
      registeredWith: registeredWith);
    await store.AddCredentialAsync(credential);

    Credential? loaded = await store.GetCredentialAsync(credential.Id);
    loaded.ShouldNotBeNull();
    loaded.Label.ShouldBe("Proton Pass");
    loaded.Nickname.ShouldBe("work laptop");
    loaded.RegisteredWith.ShouldBe(registeredWith);
    loaded.Fingerprint.ShouldBe(credential.Fingerprint);

    loaded.Rename("home desktop");
    await store.UpdateCredentialAsync(loaded);

    Credential? renamed = await store.GetCredentialAsync(credential.Id);
    renamed.ShouldNotBeNull();
    renamed.Nickname.ShouldBe("home desktop");
    renamed.Label.ShouldBe("Proton Pass");
    renamed.RegisteredWith.ShouldBe(registeredWith);
    renamed.Version.ShouldBe(1);
  }

  public async Task Update_missing_credential_fails()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);
    await Should.ThrowAsync<InvalidOperationException>(() => store.UpdateCredentialAsync(credential));
  }

  public async Task Update_rejects_PrincipalId_reparent()
  {
    if (ShouldSkip()) return;

    IPrincipalStore store = Factory.CreateStore();
    Principal source = Principal.Create(PrincipalKind.Human);
    Principal target = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(source);
    await store.AddPrincipalAsync(target);

    Credential credential = Credential.Create(source.Id, CredentialType.Passkey, [9], [9]);
    await store.AddCredentialAsync(credential);
    Credential? loaded = await store.GetCredentialAsync(credential.Id);
    loaded.ShouldNotBeNull();
    loaded.ReparentTo(target.Id);

    await Should.ThrowAsync<InvalidOperationException>(() => store.UpdateCredentialAsync(loaded));

    Credential? stored = await store.GetCredentialAsync(credential.Id);
    stored.ShouldNotBeNull();
    stored.PrincipalId.ShouldBe(source.Id);
  }

  public async Task Lists_in_ascending_CreatedAt_order()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Credential first = Credential.Create(principal.Id, CredentialType.Passkey, [1], [1]);
    await Task.Delay(TimeSpan.FromMilliseconds(5));
    Credential second = Credential.Create(principal.Id, CredentialType.AgentKey, [2], [2]);
    await Task.Delay(TimeSpan.FromMilliseconds(5));
    Credential third = Credential.Create(principal.Id, CredentialType.Passkey, [3], [3]);

    await store.AddCredentialAsync(third);
    await store.AddCredentialAsync(first);
    await store.AddCredentialAsync(second);

    IReadOnlyList<Credential> list = await store.ListCredentialsAsync(principal.Id);

    list.Count.ShouldBe(3);
    list[0].Id.ShouldBe(first.Id);
    list[1].Id.ShouldBe(second.Id);
    list[2].Id.ShouldBe(third.Id);
  }
}
