#region Purpose
// Shared IPrincipalStore contract case group CallerAheadConflict (task 104-032 dual-fixture); inherited by the
// in-memory and EF concrete fixtures so every backend pins the same semantics.
#endregion

namespace PrincipalStoreContract_;

using TimeWarp.Identity;

public abstract class CallerAheadConflict
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Ahead_of_store_throws_with_expected_greater_than_actual()
  {

    if (ShouldSkip()) return; IPrincipalStore storeA = Factory.CreateStore();
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
