#region Purpose
// Shared IPrincipalStore contract case group StalePrincipalUpdate (task 104-032 dual-fixture); inherited by the
// in-memory and EF concrete fixtures so every backend pins the same semantics.
#endregion

namespace PrincipalStoreContract_;

using TimeWarp.Identity;

public abstract class StalePrincipalUpdate
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Conflicting_update_throws_with_expected_and_actual_versions()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
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

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
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
