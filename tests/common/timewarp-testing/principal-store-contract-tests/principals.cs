#region Purpose
// Shared IPrincipalStore contract case group Principals (task 104-032 dual-fixture); inherited by the
// in-memory and EF concrete fixtures so every backend pins the same semantics.
#endregion

namespace PrincipalStoreContract_;

using TimeWarp.Identity;

public abstract class Principals
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Add_and_get_round_trips()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
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

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    await Should.ThrowAsync<InvalidOperationException>(() => store.AddPrincipalAsync(principal));
  }

  public async Task Update_persists_display_name_and_tier()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
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

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Service);
    await Should.ThrowAsync<InvalidOperationException>(() => store.UpdatePrincipalAsync(principal));
  }

  public async Task List_principals_returns_snapshots_ordered_by_created_at()
  {
    if (ShouldSkip()) return;
    IPrincipalStore store = Factory.CreateStore();

    Principal first = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(first);
    await Task.Delay(TimeSpan.FromMilliseconds(5));
    Principal second = Principal.Create(PrincipalKind.Agent);
    await store.AddPrincipalAsync(second);
    await Task.Delay(TimeSpan.FromMilliseconds(5));
    Principal third = Principal.Create(PrincipalKind.Service);
    await store.AddPrincipalAsync(third);

    IReadOnlyList<Principal> list = await store.ListPrincipalsAsync();

    list.Count.ShouldBe(3);
    list[0].Id.ShouldBe(first.Id);
    list[1].Id.ShouldBe(second.Id);
    list[2].Id.ShouldBe(third.Id);
    ReferenceEquals(list[0], first).ShouldBeFalse();
  }

  public async Task List_principals_empty_store_returns_empty()
  {
    if (ShouldSkip()) return;
    IPrincipalStore store = Factory.CreateStore();

    IReadOnlyList<Principal> list = await store.ListPrincipalsAsync();

    list.Count.ShouldBe(0);
  }
}
