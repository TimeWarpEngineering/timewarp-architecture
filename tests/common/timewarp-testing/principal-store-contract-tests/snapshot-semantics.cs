#region Purpose
// Shared IPrincipalStore contract case group SnapshotSemantics (task 104-032 dual-fixture); inherited by the
// in-memory and EF concrete fixtures so every backend pins the same semantics.
#endregion

namespace PrincipalStoreContract_;

using TimeWarp.Identity;

public abstract class SnapshotSemantics
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Get_twice_returns_distinct_but_equal_instances()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
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

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
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

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
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

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
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
