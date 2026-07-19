// Task 104-028: snapshot-on-get + Version optimistic-concurrency checks. These are deterministic
// interleavings (A does X then B does Y), not multi-threaded races — the point is to prove the
// store's check-then-act semantics are correct, not to stress-test the lock.
namespace InMemoryPrincipalStoreConcurrency_;

public class SnapshotSemantics
{
  public async Task Get_twice_returns_distinct_but_equal_instances()
  {
    var store = new InMemoryPrincipalStore();
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
    var store = new InMemoryPrincipalStore();
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
    var store = new InMemoryPrincipalStore();
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
    var store = new InMemoryPrincipalStore();
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

public class AddPersistsVersionAsIs
{
  // Port contract: "Add* persists the given instance's state as-is, including Version." Every
  // other test in this suite only ever adds a freshly Create()'d (version-0) instance, so a store
  // that silently reset Version to 0 on Add would pass everything else — this pins the nonzero case
  // by moving a Get-returned (version-1) snapshot from one store into a fresh, unrelated store.
  public async Task Add_of_nonzero_version_snapshot_persists_that_version()
  {
    var sourceStore = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await sourceStore.AddPrincipalAsync(principal);

    Principal? loaded = await sourceStore.GetPrincipalAsync(principal.Id);
    loaded.ShouldNotBeNull();
    loaded.SetDisplayName("bumped");
    await sourceStore.UpdatePrincipalAsync(loaded);

    Principal? v1Snapshot = await sourceStore.GetPrincipalAsync(principal.Id);
    v1Snapshot.ShouldNotBeNull();
    v1Snapshot.Version.ShouldBe(1);

    var freshStore = new InMemoryPrincipalStore();
    await freshStore.AddPrincipalAsync(v1Snapshot);

    Principal? reloaded = await freshStore.GetPrincipalAsync(principal.Id);
    reloaded.ShouldNotBeNull();
    reloaded.Version.ShouldBe(1);
  }
}

public class StalePrincipalUpdate
{
  public async Task Conflicting_update_throws_with_expected_and_actual_versions()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Principal? a = await store.GetPrincipalAsync(principal.Id);
    Principal? b = await store.GetPrincipalAsync(principal.Id);
    a.ShouldNotBeNull();
    b.ShouldNotBeNull();

    a.SetDisplayName("first");
    await store.UpdatePrincipalAsync(a); // stored now at version 1

    b.SetDisplayName("second"); // b still holds version 0

    ConcurrencyConflictException exception =
      await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdatePrincipalAsync(b));

    exception.EntityType.ShouldBe(typeof(Principal));
    exception.ExpectedVersion.ShouldBe(0);
    exception.ActualVersion.ShouldBe(1);
  }

  public async Task Retry_after_reGet_succeeds()
  {
    var store = new InMemoryPrincipalStore();
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

public class CallerAheadConflict
{
  // Every other conflict test in this suite is caller-BEHIND (Expected 0, Actual 1) — the version
  // check is a plain `!=`, but nothing pinned the other direction, so a regression to `<` (only
  // catching behind-callers) would have passed the whole suite. Construct a caller-AHEAD instance
  // via two independent stores: store A advances a principal to version 1; that v1 snapshot is then
  // presented to store B, which only ever saw the original version-0 principal.
  public async Task Ahead_of_store_throws_with_expected_greater_than_actual()
  {
    var storeA = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await storeA.AddPrincipalAsync(principal);

    Principal? loaded = await storeA.GetPrincipalAsync(principal.Id);
    loaded.ShouldNotBeNull();
    loaded.SetDisplayName("updated");
    await storeA.UpdatePrincipalAsync(loaded);

    Principal? ahead = await storeA.GetPrincipalAsync(principal.Id);
    ahead.ShouldNotBeNull();
    ahead.Version.ShouldBe(1);

    var storeB = new InMemoryPrincipalStore();
    await storeB.AddPrincipalAsync(principal); // storeB only ever saw version 0

    ConcurrencyConflictException exception =
      await Should.ThrowAsync<ConcurrencyConflictException>(() => storeB.UpdatePrincipalAsync(ahead));

    exception.ExpectedVersion.ShouldBe(1);
    exception.ActualVersion.ShouldBe(0);
    exception.ExpectedVersion.ShouldBeGreaterThan(exception.ActualVersion);
  }
}

public class RevokeResurrectionRace
{
  // Headline case: fails under the old shared-reference/last-write-wins store (a stale full-entity
  // overwrite could put RevokedAt back to null), passes with the version token.
  public async Task Stale_update_after_revoke_throws_and_store_stays_revoked()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Credential credential = Credential.Create(principal.Id, CredentialType.Passkey, [1], [2], "laptop");
    await store.AddCredentialAsync(credential);

    // A and B both hold pre-revoke snapshots (version 0).
    Credential? a = await store.GetCredentialAsync(credential.Id);
    Credential? b = await store.GetCredentialAsync(credential.Id);
    a.ShouldNotBeNull();
    b.ShouldNotBeNull();

    a.Revoke();
    await store.UpdateCredentialAsync(a); // stored now revoked, version 1

    // B is a stale writer (e.g. a concurrent label-editor) whose in-hand snapshot still carries the
    // pre-revoke RevokedAt = null — the version check must reject this before that null ever
    // overwrites the stored RevokedAt.
    await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdateCredentialAsync(b));

    Credential? reloaded = await store.GetCredentialAsync(credential.Id);
    reloaded.ShouldNotBeNull();
    reloaded.IsRevoked.ShouldBeTrue();
  }
}

public class QuarantineLossRace
{
  public async Task Stale_update_throws_and_store_stays_quarantined()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Principal? a = await store.GetPrincipalAsync(principal.Id);
    Principal? b = await store.GetPrincipalAsync(principal.Id);
    a.ShouldNotBeNull();
    b.ShouldNotBeNull();

    a.Quarantine();
    await store.UpdatePrincipalAsync(a);

    b.SetDisplayName("stale writer"); // b still carries IsQuarantined = false
    await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdatePrincipalAsync(b));

    Principal? reloaded = await store.GetPrincipalAsync(principal.Id);
    reloaded.ShouldNotBeNull();
    reloaded.IsQuarantined.ShouldBeTrue();
  }
}

public class TierDemotionRace
{
  public async Task Stale_update_throws_and_store_stays_at_promoted_tier()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Principal? a = await store.GetPrincipalAsync(principal.Id);
    Principal? b = await store.GetPrincipalAsync(principal.Id);
    a.ShouldNotBeNull();
    b.ShouldNotBeNull();

    a.Promote(TrustTier.Funded);
    await store.UpdatePrincipalAsync(a);

    b.SetDisplayName("stale writer"); // b still carries TrustTier.Provisional
    await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdatePrincipalAsync(b));

    Principal? reloaded = await store.GetPrincipalAsync(principal.Id);
    reloaded.ShouldNotBeNull();
    reloaded.TrustTier.ShouldBe(TrustTier.Funded);
  }
}

public class AttachBumpsPrincipalVersion
{
  public async Task Pre_attach_snapshot_update_conflicts_after_first_credential()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Principal? preAttach = await store.GetPrincipalAsync(principal.Id);
    preAttach.ShouldNotBeNull();

    Credential credential = Credential.Create(principal.Id, CredentialType.Passkey, [1], [2]);
    await store.AddCredentialAsync(credential); // bumps stored principal to version 1, Keyed

    preAttach.SetDisplayName("stale");
    await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdatePrincipalAsync(preAttach));
  }

  public async Task Second_credential_add_does_not_bump_version_again()
  {
    var store = new InMemoryPrincipalStore();
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

public class CallerInstanceNotAdvanced
{
  public async Task Version_unchanged_on_callers_instance_after_successful_update()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    principal.SetDisplayName("first");
    await store.UpdatePrincipalAsync(principal);

    principal.Version.ShouldBe(0);
  }

  public async Task Second_update_with_same_instance_throws()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    principal.SetDisplayName("first");
    await store.UpdatePrincipalAsync(principal);

    principal.SetDisplayName("second");
    await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdatePrincipalAsync(principal));
  }
}
