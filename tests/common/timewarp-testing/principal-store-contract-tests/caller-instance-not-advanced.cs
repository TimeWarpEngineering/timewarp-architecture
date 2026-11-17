#region Purpose
// Shared IPrincipalStore contract case group CallerInstanceNotAdvanced (task 104-032 dual-fixture); inherited by the
// in-memory and EF concrete fixtures so every backend pins the same semantics.
#endregion

namespace PrincipalStoreContract_;

using TimeWarp.Identity;

public abstract class CallerInstanceNotAdvanced
{
  protected abstract IPrincipalStoreFactory Factory { get; }

  /// <summary>Soft-skip hook for hosts without Postgres (CI must throw inside instead).</summary>
  protected virtual bool ShouldSkip() => false;

  public async Task Version_unchanged_on_callers_instance_after_successful_update()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    principal.SetDisplayName("first");
    await store.UpdatePrincipalAsync(principal);

    principal.Version.ShouldBe(0);
  }

  public async Task Second_update_with_same_instance_throws()
  {

    if (ShouldSkip()) return; IPrincipalStore store = Factory.CreateStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    principal.SetDisplayName("first");
    await store.UpdatePrincipalAsync(principal);

    principal.SetDisplayName("second");
    await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdatePrincipalAsync(principal));
  }
}
