#region Purpose
// IActionCatalog (TimeWarp.State [CatalogAction]) as composed by the SPA's ConfigureServices:
// the tagged roster, an entry executing through the store, and excluded actions staying out.
#endregion

#region Design
// Task 239-002. ExpectedNames is the full roster on purpose — adding or dropping a
// [CatalogAction] must be a deliberate edit here, not a silent palette/agent surface change.
// Execution goes through ActionCatalogEntry.Execute (the path the Ctrl-K palette and agent tools
// use), then re-reads CounterState via Store.GetState: dispatch replaces the state instance.
#endregion

namespace ActionCatalog_;

using TimeWarp.Architecture.Features;

[TestTag("Integration")]
public class ActionCatalog_Should
{
  private static SpaSessionFixture? Session;
  private static AspireSpaTestApplication? Spa;

  private static readonly string[] ExpectedNames =
  [
    "Counter.IncrementCounter",
    "Credentials.AddExistingPasskey",
    "Credentials.AddPasskey",
    // Task 279: the follow-up refresh of every server-offered credential action (Agent, parameterless).
    "Credentials.FetchCredentials",
    "Credentials.LinkMicrosoft365",
    // Task 281: offered under the [Offerable] contracts' generated OfferName (<Slice>.<Operation>).
    "Identity.RenameCredential",
    "Identity.RevokeCredential",
    "Profile.SignOut",
    "Profile.UpdateProfile",
    "Role.CreateRole",
    "SiteSettings.UpdateSiteSettings",
  ];

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ActionCatalog_Should>();

  public static async Task SetupOnce()
  {
    Session = await SessionFixture.GetAsync<SpaSessionFixture>();
    Spa = new AspireSpaTestApplication(Session.Inner);
  }

  public static Task CleanUpOnce()
  {
    // Session-owned: the Jaribu session hook disposes SpaSessionFixture; do not dispose here.
    Session = null;
    Spa = null;
    return Task.CompletedTask;
  }

  public static Task Enumerate_Expected_Names()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog actionCatalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();

    actionCatalog.Entries.Select(entry => entry.Name).OrderBy(name => name, StringComparer.Ordinal)
      .ShouldBe(ExpectedNames);
    return Task.CompletedTask;
  }

  public static Task Carry_PermissionIds_And_Visibility()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog actionCatalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();

    ActionCatalogEntry createRole = actionCatalog.Find("Role.CreateRole").ShouldNotBeNull();
    createRole.Permissions.ShouldBe([PermissionIds.AdminRolesManage]);
    createRole.Visibility.ShouldBe(ActionVisibility.Agent);

    ActionCatalogEntry signOut = actionCatalog.Find("Profile.SignOut").ShouldNotBeNull();
    signOut.Permissions.ShouldBeEmpty();
    signOut.Visibility.ShouldBe(ActionVisibility.Human);
    signOut.DisplayName.ShouldBeNull("The generated \"Profile: Sign out\" reads right; no authored label.");

    // Task 260: the Settings "Link Microsoft 365" button is this action; same gate as the page CTA.
    ActionCatalogEntry linkMicrosoft365 = actionCatalog.Find("Credentials.LinkMicrosoft365").ShouldNotBeNull();
    linkMicrosoft365.Permissions.ShouldBe([PermissionIds.CredentialManageSelf]);
    linkMicrosoft365.Visibility.ShouldBe(ActionVisibility.Human);
    linkMicrosoft365.Description.ShouldNotBeNullOrWhiteSpace();
    linkMicrosoft365.DisplayName.ShouldBe("Link Microsoft 365");
    return Task.CompletedTask;
  }

  public static async Task Execute_IncrementCounter_Through_Store()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog actionCatalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    ActionCatalogEntry incrementCounter = actionCatalog.Find("Counter.IncrementCounter").ShouldNotBeNull();

    scope.Store.GetState<CounterState>().Initialize(count: 10);

    // "5" is the string form an agent tool would supply; the catalog converts it to int.
    await incrementCounter.Execute(scope.Store, ["5"], CancellationToken.None);

    scope.Store.GetState<CounterState>().Count.ShouldBe(15);
  }

  [Input("Superhero.FetchSuperhero")]
  [Input("Profile.ClearProfileData")]
  [Input("Application.FiveSecondTask")]
  [Input("Application.TwoSecondTask")]
  [Input("Counter.ThrowException")]
  [Input("Chat.ServerToClientMessage")]
  [Input("Theme.Update")]
  public static Task Exclude_Untagged_Action(string name)
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog actionCatalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();

    actionCatalog.Find(name).ShouldBeNull();
    return Task.CompletedTask;
  }
}
