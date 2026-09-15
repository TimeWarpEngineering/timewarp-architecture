#region Purpose
// In-memory ISiteSettingsStore: get/add/update/concurrency.
#endregion

namespace SiteSettingsStore_;

using TimeWarp.Foundation.Entities;

public class InMemory_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<InMemory_Given_>();

  public static async Task Get_Empty_Should_Return_Null()
  {
    InMemorySiteSettingsStore store = new();
    SiteSettings? found = await store.GetAsync();
    found.ShouldBeNull();
  }

  public static async Task Add_Then_Get_Should_RoundTrip()
  {
    InMemorySiteSettingsStore store = new();
    Guid tenant = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
    SiteSettings created = SiteSettings.Create(
      entraSignInEnabled: true,
      entraAllowBootstrap: true,
      entraTrustedTenants: [tenant],
      passkeyPromptMode: PasskeyPromptMode.Required);

    await store.AddAsync(created);
    SiteSettings? found = await store.GetAsync();

    found.ShouldNotBeNull();
    found!.EntraSignInEnabled.ShouldBeTrue();
    found.EntraAllowBootstrap.ShouldBeTrue();
    found.EntraTrustedTenants.ShouldBe([tenant]);
    found.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Required);
    found.Version.ShouldBe(0);
    ReferenceEquals(found, created).ShouldBeFalse();
  }

  public static async Task Duplicate_Add_Should_Throw()
  {
    InMemorySiteSettingsStore store = new();
    await store.AddAsync(SiteSettings.Create());
    await Should.ThrowAsync<InvalidOperationException>(() => store.AddAsync(SiteSettings.Create()));
  }

  public static async Task Update_Should_Advance_Version()
  {
    InMemorySiteSettingsStore store = new();
    await store.AddAsync(SiteSettings.Create());
    SiteSettings? current = await store.GetAsync();
    current.ShouldNotBeNull();
    current!.ReplacePolicy(true, false, [], PasskeyPromptMode.Soft);
    await store.UpdateAsync(current);

    SiteSettings? after = await store.GetAsync();
    after.ShouldNotBeNull();
    after!.EntraSignInEnabled.ShouldBeTrue();
    after.Version.ShouldBe(EntityVersion.Next(0));
  }

  public static async Task Stale_Update_Should_Throw_And_Leave_Store()
  {
    InMemorySiteSettingsStore store = new();
    await store.AddAsync(SiteSettings.Create());
    SiteSettings? a = await store.GetAsync();
    SiteSettings? b = await store.GetAsync();
    a.ShouldNotBeNull();
    b.ShouldNotBeNull();
    a!.ReplacePolicy(true, false, [], PasskeyPromptMode.Soft);
    await store.UpdateAsync(a);

    b!.ReplacePolicy(false, true, [], PasskeyPromptMode.Required);
    ConcurrencyConflictException exception =
      await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdateAsync(b));
    exception.ExpectedVersion.ShouldBe(0);
    exception.ActualVersion.ShouldBe(1);

    SiteSettings? stored = await store.GetAsync();
    stored.ShouldNotBeNull();
    stored!.EntraSignInEnabled.ShouldBeTrue();
    stored.EntraAllowBootstrap.ShouldBeFalse();
    stored.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Soft);
  }
}
