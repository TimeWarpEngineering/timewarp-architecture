#region Purpose
// Shared IPrincipalStore contract case group AddPersistsVersionAsIs (task 104-032 dual-fixture); inherited by the
// in-memory and EF concrete fixtures so every backend pins the same semantics.
#endregion

namespace PrincipalStoreContract_;

using TimeWarp.Identity;

public abstract class AddPersistsVersionAsIs
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Add_of_nonzero_version_snapshot_persists_that_version()
  {

    if (ShouldSkip()) return; IPrincipalStore sourceStore = Factory.CreateStore();
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
