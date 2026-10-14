#region Purpose
// SiteSettings.AssistantName (task 361): default Akili, rename trims, invalid names throw, rename survives the store.
#endregion

namespace SiteSettingsAssistantName_;

using TimeWarp.Jaribu;

public class SiteSettings_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SiteSettings_Given_>();

  public static Task Create_Should_Default_To_Akili()
  {
    SiteSettings.Create().AssistantName.ShouldBe("Akili");
    AssistantNameRules.Default.ShouldBe("Akili");
    return Task.CompletedTask;
  }

  public static Task Rename_Should_Trim()
  {
    SiteSettings settings = SiteSettings.Create();
    settings.RenameAssistant("  Zawadi  ");
    settings.AssistantName.ShouldBe("Zawadi");
    return Task.CompletedTask;
  }

  [Input("")]
  [Input("   ")]
  [Input("123456789012345678901234567890123")]
  [Input("<script>")]
  [Input("Ak\u0000ili")]
  [Input("Ak\u2028ili")]
  [Input("Ak\u202Eili")]
  [Input("Ak\u200Bili")]
  public static Task Rename_Should_Reject_Invalid_Names(string name)
  {
    SiteSettings settings = SiteSettings.Create();
    Should.Throw<ArgumentException>(() => settings.RenameAssistant(name));
    settings.AssistantName.ShouldBe("Akili");
    return Task.CompletedTask;
  }

  public static async Task Rename_Should_Survive_Update_And_Get()
  {
    InMemorySiteSettingsStore store = new();
    await store.AddAsync(SiteSettings.Create());
    SiteSettings current = (await store.GetAsync())!;
    current.RenameAssistant("Zawadi");
    await store.UpdateAsync(current);

    SiteSettings? found = await store.GetAsync();
    found.ShouldNotBeNull();
    found.AssistantName.ShouldBe("Zawadi");
    found.Version.ShouldBe(1);
  }
}
