// Shared IPrincipalStore contract cases (task 104-032 dual-fixture). Concrete fixtures:
//   - InMemoryPrincipalStoreContract in this project
//   - EfPrincipalStoreContract in web-infrastructure-tests (Testcontainers Postgres)
// Semantics pinned here must hold for every backend — snapshot-on-get, Version CAS,
// first-credential tier bump, handle uniqueness, list ordering.

namespace PrincipalStoreContract_;

/// <summary>Factory for an empty, independent <see cref="IPrincipalStore"/> instance.</summary>
public interface IPrincipalStoreFactory
{
  IPrincipalStore CreateStore();
}

public abstract class Principals
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Add_and_get_round_trips()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);

    await store.AddPrincipalAsync(principal);
    Principal? loaded = await store.GetPrincipalAsync(principal.Id);

    loaded.ShouldNotBeNull();
    loaded.Id.ShouldBe(principal.Id);
    loaded.Kind.ShouldBe(PrincipalKind.Human);
    loaded.TrustTier.ShouldBe(TrustTier.Provisional);
  }

  public async Task Duplicate_principal_id_fails()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    await Should.ThrowAsync<InvalidOperationException>(() => store.AddPrincipalAsync(principal));
  }

  public async Task Update_persists_display_name_and_tier()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Agent);
    await store.AddPrincipalAsync(principal);

    principal.SetDisplayName("bot");
    principal.Promote(TrustTier.Funded);
    await store.UpdatePrincipalAsync(principal);

    Principal? loaded = await store.GetPrincipalAsync(principal.Id);
    loaded.ShouldNotBeNull();
    loaded.DisplayName.ShouldBe("bot");
    loaded.TrustTier.ShouldBe(TrustTier.Funded);
    loaded.Version.ShouldBe(1);
    principal.Version.ShouldBe(0);
  }

  public async Task Update_missing_principal_fails()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Service);
    await Should.ThrowAsync<InvalidOperationException>(() => store.UpdatePrincipalAsync(principal));
  }
}

public abstract class Credentials
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task First_credential_promotes_provisional_to_keyed()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
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

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
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

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
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

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
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

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
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

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    byte[] handle = [7, 7];
    await store.AddCredentialAsync(Credential.Create(principal.Id, CredentialType.Passkey, handle, [1]));

    Credential duplicate = Credential.Create(principal.Id, CredentialType.Passkey, handle.ToArray(), [2]);
    await Should.ThrowAsync<InvalidOperationException>(() => store.AddCredentialAsync(duplicate));
  }

  public async Task Same_handle_different_type_is_allowed()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    byte[] handle = [8, 8];
    await store.AddCredentialAsync(Credential.Create(principal.Id, CredentialType.Passkey, handle, [1]));
    await store.AddCredentialAsync(Credential.Create(principal.Id, CredentialType.AgentKey, handle.ToArray(), [2]));

    (await store.ListCredentialsAsync(principal.Id)).Count.ShouldBe(2);
  }

  public async Task Missing_principal_fails_credential_add()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);
    await Should.ThrowAsync<InvalidOperationException>(() => store.AddCredentialAsync(credential));
  }

  public async Task List_excludes_revoked_by_default()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
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

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Service);
    await store.AddPrincipalAsync(principal);

    Credential credential = Credential.Create(principal.Id, CredentialType.AgentKey, [5], [6]);
    await store.AddCredentialAsync(credential);

    Credential? loaded = await store.GetCredentialAsync(credential.Id);
    loaded.ShouldNotBeNull();
    loaded.PrincipalId.ShouldBe(principal.Id);
    loaded.Id.ShouldBe(credential.Id);
  }

  public async Task Update_missing_credential_fails()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);
    await Should.ThrowAsync<InvalidOperationException>(() => store.UpdateCredentialAsync(credential));
  }

  public async Task Lists_in_ascending_CreatedAt_order()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
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

public abstract class SnapshotSemantics
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Get_twice_returns_distinct_but_equal_instances()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Principal? a = await store.GetPrincipalAsync(principal.Id);
    Principal? b = await store.GetPrincipalAsync(principal.Id);

    a.ShouldNotBeNull();
    b.ShouldNotBeNull();
    ReferenceEquals(a, b).ShouldBeFalse();
    a.ShouldBe(b);
  }

  public async Task Mutating_a_snapshot_is_invisible_until_update()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Principal? snapshot = await store.GetPrincipalAsync(principal.Id);
    snapshot.ShouldNotBeNull();
    snapshot.SetDisplayName("mutated");

    Principal? reloaded = await store.GetPrincipalAsync(principal.Id);
    reloaded.ShouldNotBeNull();
    reloaded.DisplayName.ShouldBeNull();
  }

  public async Task Credential_byte_arrays_are_independent_across_snapshots()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Credential credential = Credential.Create(principal.Id, CredentialType.Passkey, [1, 2, 3], [4, 5, 6]);
    await store.AddCredentialAsync(credential);

    Credential? a = await store.GetCredentialAsync(credential.Id);
    Credential? b = await store.GetCredentialAsync(credential.Id);
    a.ShouldNotBeNull();
    b.ShouldNotBeNull();

    byte[] handleFromA = a.Handle;
    handleFromA[0] = 99;

    b.Handle[0].ShouldBe((byte)1);
  }

  public async Task Version_is_zero_after_create_and_one_after_update()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    principal.Version.ShouldBe(0);
    await store.AddPrincipalAsync(principal);

    Principal? loaded = await store.GetPrincipalAsync(principal.Id);
    loaded.ShouldNotBeNull();
    loaded.Version.ShouldBe(0);

    loaded.SetDisplayName("x");
    await store.UpdatePrincipalAsync(loaded);

    Principal? reloaded = await store.GetPrincipalAsync(principal.Id);
    reloaded.ShouldNotBeNull();
    reloaded.Version.ShouldBe(1);
  }
}

public abstract class AddPersistsVersionAsIs
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Add_of_nonzero_version_snapshot_persists_that_version()
  {

    if (ShouldSkip()) return;    IPrincipalStore sourceStore = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await sourceStore.AddPrincipalAsync(principal);

    Principal? loaded = await sourceStore.GetPrincipalAsync(principal.Id);
    loaded.ShouldNotBeNull();
    loaded.SetDisplayName("bumped");
    await sourceStore.UpdatePrincipalAsync(loaded);

    Principal? v1Snapshot = await sourceStore.GetPrincipalAsync(principal.Id);
    v1Snapshot.ShouldNotBeNull();
    v1Snapshot.Version.ShouldBe(1);

    IPrincipalStore freshStore = Factory.CreateStore();
    await freshStore.AddPrincipalAsync(v1Snapshot);

    Principal? reloaded = await freshStore.GetPrincipalAsync(principal.Id);
    reloaded.ShouldNotBeNull();
    reloaded.Version.ShouldBe(1);
  }
}

public abstract class StalePrincipalUpdate
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Conflicting_update_throws_with_expected_and_actual_versions()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Principal? a = await store.GetPrincipalAsync(principal.Id);
    Principal? b = await store.GetPrincipalAsync(principal.Id);
    a.ShouldNotBeNull();
    b.ShouldNotBeNull();

    a.SetDisplayName("first");
    await store.UpdatePrincipalAsync(a);

    b.SetDisplayName("second");

    ConcurrencyConflictException exception =
      await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdatePrincipalAsync(b));

    exception.EntityType.ShouldBe(typeof(Principal));
    exception.ExpectedVersion.ShouldBe(0);
    exception.ActualVersion.ShouldBe(1);
  }

  public async Task Retry_after_reGet_succeeds()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Principal? a = await store.GetPrincipalAsync(principal.Id);
    Principal? b = await store.GetPrincipalAsync(principal.Id);
    a.ShouldNotBeNull();
    b.ShouldNotBeNull();

    a.SetDisplayName("first");
    await store.UpdatePrincipalAsync(a);

    b.SetDisplayName("second");
    await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdatePrincipalAsync(b));

    Principal? fresh = await store.GetPrincipalAsync(principal.Id);
    fresh.ShouldNotBeNull();
    fresh.SetDisplayName("second-retry");
    await store.UpdatePrincipalAsync(fresh);

    Principal? final = await store.GetPrincipalAsync(principal.Id);
    final.ShouldNotBeNull();
    final.DisplayName.ShouldBe("second-retry");
    final.Version.ShouldBe(2);
  }
}

public abstract class CallerAheadConflict
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Ahead_of_store_throws_with_expected_greater_than_actual()
  {

    if (ShouldSkip()) return;    IPrincipalStore storeA = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await storeA.AddPrincipalAsync(principal);

    Principal? loaded = await storeA.GetPrincipalAsync(principal.Id);
    loaded.ShouldNotBeNull();
    loaded.SetDisplayName("updated");
    await storeA.UpdatePrincipalAsync(loaded);

    Principal? ahead = await storeA.GetPrincipalAsync(principal.Id);
    ahead.ShouldNotBeNull();
    ahead.Version.ShouldBe(1);

    IPrincipalStore storeB = Factory.CreateStore();
    await storeB.AddPrincipalAsync(principal);

    ConcurrencyConflictException exception =
      await Should.ThrowAsync<ConcurrencyConflictException>(() => storeB.UpdatePrincipalAsync(ahead));

    exception.ExpectedVersion.ShouldBe(1);
    exception.ActualVersion.ShouldBe(0);
    exception.ExpectedVersion.ShouldBeGreaterThan(exception.ActualVersion);
  }
}

public abstract class RevokeResurrectionRace
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Stale_update_after_revoke_throws_and_store_stays_revoked()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
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

public abstract class QuarantineLossRace
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Stale_update_throws_and_store_stays_quarantined()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Principal? a = await store.GetPrincipalAsync(principal.Id);
    Principal? b = await store.GetPrincipalAsync(principal.Id);
    a.ShouldNotBeNull();
    b.ShouldNotBeNull();

    a.Quarantine();
    await store.UpdatePrincipalAsync(a);

    b.SetDisplayName("stale writer");
    await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdatePrincipalAsync(b));

    Principal? reloaded = await store.GetPrincipalAsync(principal.Id);
    reloaded.ShouldNotBeNull();
    reloaded.IsQuarantined.ShouldBeTrue();
  }
}

public abstract class TierDemotionRace
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Stale_update_throws_and_store_stays_at_promoted_tier()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Principal? a = await store.GetPrincipalAsync(principal.Id);
    Principal? b = await store.GetPrincipalAsync(principal.Id);
    a.ShouldNotBeNull();
    b.ShouldNotBeNull();

    a.Promote(TrustTier.Funded);
    await store.UpdatePrincipalAsync(a);

    b.SetDisplayName("stale writer");
    await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdatePrincipalAsync(b));

    Principal? reloaded = await store.GetPrincipalAsync(principal.Id);
    reloaded.ShouldNotBeNull();
    reloaded.TrustTier.ShouldBe(TrustTier.Funded);
  }
}

public abstract class AttachBumpsPrincipalVersion
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Pre_attach_snapshot_update_conflicts_after_first_credential()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
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

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
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

public abstract class CallerInstanceNotAdvanced
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Version_unchanged_on_callers_instance_after_successful_update()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    principal.SetDisplayName("first");
    await store.UpdatePrincipalAsync(principal);

    principal.Version.ShouldBe(0);
  }

  public async Task Second_update_with_same_instance_throws()
  {

    if (ShouldSkip()) return;    IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    principal.SetDisplayName("first");
    await store.UpdatePrincipalAsync(principal);

    principal.SetDisplayName("second");
    await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdatePrincipalAsync(principal));
  }
}
