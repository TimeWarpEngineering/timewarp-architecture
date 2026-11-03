#region Purpose
// Shared IPrincipalStore contract case group QuarantineLossRace (task 104-032 dual-fixture); inherited by the
// in-memory and EF concrete fixtures so every backend pins the same semantics.
#endregion

namespace PrincipalStoreContract_;

using TimeWarp.Identity;

public abstract class QuarantineLossRace
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Stale_update_throws_and_store_stays_quarantined()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
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
