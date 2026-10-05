#region Purpose
// Hypermedia approach B on the real Credentials pages (task 279): the SPA runs only what the server's
// GetCredentials offers name — through the catalog, with the real Credentials actions — refreshes to
// the follow-up set, refuses unknown names, bad arguments, stale or forged rows and hidden or
// unpermitted catalog entries (275 review M4), and contributes Ctrl-K rows only on Settings and Passkeys.
#endregion

#region Design
// C-create in-proc SPA ServiceProvider per test (AGENTS.md default): real TimeWarp.State pipeline,
// real IActionCatalog, real CommandPaletteContext + CredentialsContextSource, a TestNavigationManager
// for the current path, and the shared ScriptedCredentialsApiService standing in for the BFF. The
// script applies the server's offer rule and the real effects (revoke/rename), so "the follow-up
// payload changes the set" is observed end to end on the client; the rule itself, against the real
// handler, is pinned in web-server-integration-tests (credential-offers-tests). This file proves the
// client runs what it is given and nothing else, through the entry points the pages and the palette
// use: CredentialOfferRows.RunAsync (page buttons, with input) and CommandPaletteRunner.RunAsync
// (palette). Permissions are a constructor argument so the M4 "unpermitted" case uses a real
// IAuthorizationService over the app's real policies.
#endregion

namespace CredentialOffers_;

using System.Security.Claims;
using FakeItEasy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Features.Identity;
using TimeWarp.Foundation.Features;
using TimeWarp.Identity;
using CredentialSummary = TimeWarp.Architecture.Features.Identity.GetCredentials.CredentialSummary;

[TestTag("Integration")]
public class CredentialOffers_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CredentialOffers_Should_>();

  // --- vocabulary ---------------------------------------------------------------------------

  public static Task Name_Only_Catalog_Entries_A_Person_May_Run()
  {
    using OffersSpa spa = new();
    IActionCatalog catalog = spa.ServiceProvider.GetRequiredService<IActionCatalog>();

    foreach (string name in OfferedActionNames.All)
    {
      ActionCatalogEntry entry = catalog.Find(name).ShouldNotBeNull($"the server may offer {name}");
      entry.Visibility.ShouldBeOneOf([ActionVisibility.Human, ActionVisibility.Both], $"{name} must pass the M4 visibility gate");
    }

    foreach (string name in new[] { OfferedActionNames.RevokeCredential, OfferedActionNames.RenameCredential })
    {
      catalog.Find(name)!.Parameters.Select(static parameter => parameter.Name).ShouldContain(OfferedActionNames.CredentialIdArgument);
    }

    catalog.Find(OfferedActionNames.RenameCredential)!.Parameters
      .Where(static parameter => parameter.IsRequired && parameter.Name != OfferedActionNames.CredentialIdArgument)
      .Select(static parameter => parameter.Name)
      .ShouldBe([CredentialOfferRows.NicknameParameter]);
    catalog.Find(CredentialsState.FetchCredentialsActionSet.CatalogName).ShouldNotBeNull()
      .Parameters.ShouldAllBe(static parameter => !parameter.IsRequired);
    return Task.CompletedTask;
  }

  // --- offers drive the buttons -------------------------------------------------------------

  public static async Task Hold_The_Servers_Offers_Not_A_Client_Rule()
  {
    using OffersSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    CredentialSummary passkey = spa.Api.AddPasskey("Work laptop", "aaaa1111");
    CredentialSummary agentKey = spa.Api.Add(ScriptedCredentialsApiService.Active(CredentialType.AgentKey, "ganda"));

    await OpenAsync(scope, SettingsPath);

    CredentialsState state = scope.Store.GetState<CredentialsState>();
    state.IsOffered(OfferedActionNames.RevokeCredential, passkey.Id.Value).ShouldBeTrue("the server counts the agent key too");
    state.IsOffered(OfferedActionNames.RevokeCredential, agentKey.Id.Value).ShouldBeTrue();
    state.IsOffered(OfferedActionNames.LinkMicrosoft365, credentialId: null).ShouldBeFalse();

    // Whatever the client could count, it shows only what the server offers.
    spa.Api.Credentials.Add(ScriptedCredentialsApiService.Active(CredentialType.Passkey, "phone"));
    spa.Api.Credentials.RemoveAll(static credential => credential.Type == CredentialType.AgentKey);
    await scope.Send(new CredentialsState.FetchCredentialsActionSet.Action());
    scope.Store.GetState<CredentialsState>().Offers.Count(static offer => offer.Name == OfferedActionNames.RevokeCredential).ShouldBe(2);
  }

  public static async Task Run_The_Offered_Revoke_And_Refresh_To_The_Follow_Up_Set()
  {
    using OffersSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    CredentialSummary first = spa.Api.AddPasskey("Work laptop", "aaaa1111");
    spa.Api.AddPasskey(nickname: null, "bbbb2222");
    await OpenAsync(scope, SettingsPath);

    CommandPaletteRow revokeFirst = (await PaletteRows(scope)).Single(row =>
      row.Name.StartsWith("Credentials: Revoke", StringComparison.Ordinal) && row.Name.Contains(first.Fingerprint, StringComparison.Ordinal));
    await RunAsync(scope, revokeFirst);

    // The real action ran: its request, its outcome notification, then the runner's follow-up fetch.
    spa.Api.Requests.OfType<RevokeCredential.Command>().Single().CredentialId.ShouldBe(first.Id.Value);
    Messages(scope).ShouldContain("Credential revoked.");
    spa.Api.Requests.OfType<GetCredentials.Query>().Count().ShouldBe(2);
    CredentialsState state = scope.Store.GetState<CredentialsState>();
    state.ActivePasskeys.Count.ShouldBe(1);
    state.Offers.ShouldNotContain(static offer => offer.Name == OfferedActionNames.RevokeCredential);
    (await PaletteRows(scope)).ShouldNotContain(static row => row.Name.StartsWith("Credentials: Revoke", StringComparison.Ordinal));
  }

  public static async Task Rename_With_User_Input_For_The_Unbound_Nickname()
  {
    using OffersSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    CredentialSummary only = spa.Api.AddPasskey("Work laptop", "aaaa1111");
    await OpenAsync(scope, PasskeysPath);
    IActionCatalog catalog = Catalog(spa);
    CredentialsState state = scope.Store.GetState<CredentialsState>();
    CredentialOffer rename = state.FindOffer(OfferedActionNames.RenameCredential, only.Id.Value).ShouldNotBeNull();

    CredentialOfferRows.UnboundParameters(catalog.Find(rename.Name), rename).ShouldBe([CredentialOfferRows.NicknameParameter]);
    CredentialOfferRows.Row(rename, state.Credentials!, catalog).RequiresInput.ShouldBeTrue();
    (await PaletteRows(scope)).ShouldNotContain(static row => row.Target == OfferedActionNames.RenameCredential, "the palette has no argument UI");

    await RunOfferAsync(scope, OfferedActionNames.RenameCredential, only.Id.Value, CredentialOfferRows.NicknameInput("Desk key"));

    spa.Api.Requests.OfType<RenameCredential.Command>().Single().Nickname.ShouldBe("Desk key");
    Messages(scope).ShouldContain("Nickname saved.");
    scope.Store.GetState<CredentialsState>().Credentials!.Single(credential => credential.Id == only.Id).Nickname.ShouldBe("Desk key");
  }

  public static async Task Refuse_Input_That_Overrides_An_Offered_Argument()
  {
    using OffersSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    CredentialSummary first = spa.Api.AddPasskey("Work laptop", "aaaa1111");
    CredentialSummary second = spa.Api.AddPasskey(nickname: null, "bbbb2222");
    await OpenAsync(scope, SettingsPath);
    Dictionary<string, JsonElement> input = new()
    {
      [CredentialOfferRows.NicknameParameter] = JsonSerializer.SerializeToElement("x"),
      [OfferedActionNames.CredentialIdArgument] = JsonSerializer.SerializeToElement(second.Id.Value),
    };

    await RunOfferAsync(scope, OfferedActionNames.RenameCredential, first.Id.Value, input);

    spa.Api.Requests.OfType<RenameCredential.Command>().ShouldBeEmpty();
    Warnings(scope).ShouldContain(static title => title.Contains("cannot replace the offered 'credentialId'", StringComparison.Ordinal));
  }

  // --- fail closed --------------------------------------------------------------------------

  public static async Task Refuse_An_Action_The_Server_Does_Not_Offer()
  {
    using OffersSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    CredentialSummary only = spa.Api.AddPasskey("Work laptop", "aaaa1111");
    await OpenAsync(scope, SettingsPath);
    int before = spa.Api.Requests.Count;

    // The page's own entry point: no Revoke offer for the last credential, so nothing runs.
    await RunOfferAsync(scope, OfferedActionNames.RevokeCredential, only.Id.Value, input: null);
    // A hand-built row naming it is refused by the same gate.
    CommandPaletteRow forged = new
    (
      "Credentials: Revoke",
      "",
      CommandPaletteRowKind.Contextual,
      OfferedActionNames.RevokeCredential,
      $"{{\"credentialId\":\"{only.Id.Value:D}\"}}",
      CredentialsState.FetchCredentialsActionSet.CatalogName
    );
    await RunAsync(scope, forged);

    spa.Api.Requests.Count.ShouldBe(before, "nothing is sent, not even the follow-up");
    Warnings(scope).Count(static title => title == "Credentials: Revoke is not offered here now.").ShouldBe(1);
    Warnings(scope).ShouldContain(static title => title.EndsWith("is not offered here now.", StringComparison.Ordinal) && title.Contains(OfferedActionNames.RevokeCredential, StringComparison.Ordinal));
  }

  public static async Task Refuse_An_Unknown_Catalog_Name()
  {
    using OffersSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.AddPasskey("Work laptop", "aaaa1111");
    spa.Api.ExtraOffers.Add(OfferedAction.ForPage("Credentials.DeleteEverything", "Delete everything"));
    await OpenAsync(scope, SettingsPath);

    CommandPaletteRow unknown = (await PaletteRows(scope)).Single(static row => row.Target == "Credentials.DeleteEverything");
    int before = spa.Api.Requests.Count;
    await RunAsync(scope, unknown);

    spa.Api.Requests.Count.ShouldBe(before, "nothing is sent, not even the follow-up");
    Warnings(scope).ShouldContain(static title => title.Contains("is not an action this app knows", StringComparison.Ordinal));
  }

  [Input("{\"credentialId\":null}")]
  [Input("{\"credentialId\":\"not-a-guid\"}")]
  [Input("{\"credentialId\":\"6f1c1f43-3f4e-4f43-9f43-5f1c1f433f4e\",\"extra\":1}")]
  [Input("{}")]
  public static async Task Refuse_Arguments_That_Do_Not_Bind(string argumentsJson)
  {
    using OffersSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.AddPasskey("Work laptop", "aaaa1111");
    spa.Api.AddPasskey(nickname: null, "bbbb2222");
    spa.Api.ExtraOffers.Add(new OfferedAction(OfferedActionNames.RevokeCredential, "Bad revoke", subject: null,
      JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson)!));
    await OpenAsync(scope, SettingsPath);

    // From the contributed set (an offer leaving credentialId unbound is a button-only row).
    CommandPaletteRow bad = Context(scope).Rows().Single(static row => row.Name == "Credentials: Bad revoke");
    await CommandPaletteRunner.RunContextualAsync(bad, input: null, scope.Store, Catalog(spa), Context(scope), CancellationToken.None);

    spa.Api.Requests.OfType<RevokeCredential.Command>().ShouldBeEmpty();
    Warnings(scope).ShouldContain(static title => title.StartsWith("Credentials: Bad revoke was refused:", StringComparison.Ordinal));
  }

  // --- M4: visibility and permissions before Execute ----------------------------------------

  public static async Task Refuse_An_Offer_Naming_An_Agent_Only_Entry()
  {
    using OffersSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.AddPasskey("Work laptop", "aaaa1111");
    // A real catalog entry, but Visibility Agent: a server offer must not reach it.
    spa.Api.ExtraOffers.Add(OfferedAction.ForPage(CredentialsState.FetchCredentialsActionSet.CatalogName, "Refresh"));
    await OpenAsync(scope, SettingsPath);
    CommandPaletteRow hidden = (await PaletteRows(scope)).Single(static row => row.Target == CredentialsState.FetchCredentialsActionSet.CatalogName);
    int before = spa.Api.Requests.Count;

    await RunAsync(scope, hidden);

    spa.Api.Requests.Count.ShouldBe(before);
    Warnings(scope).ShouldContain(static title => title.Contains("is not an action people run here", StringComparison.Ordinal));
  }

  public static async Task Refuse_An_Offer_The_Principal_Is_Not_Permitted_To_Run()
  {
    // Signed in, but without credential.manage.self: the offered Revoke is refused before Execute.
    using OffersSpa spa = new([.. PermissionIds.All.Where(static permission => permission != PermissionIds.CredentialManageSelf)]);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    CredentialSummary first = spa.Api.AddPasskey("Work laptop", "aaaa1111");
    spa.Api.AddPasskey(nickname: null, "bbbb2222");
    await OpenAsync(scope, SettingsPath);

    await RunOfferAsync(scope, OfferedActionNames.RevokeCredential, first.Id.Value, input: null);

    spa.Api.Requests.OfType<RevokeCredential.Command>().ShouldBeEmpty();
    Warnings(scope).ShouldContain(static title => title.Contains("you are not permitted to run 'Credentials.RevokeCredential'", StringComparison.Ordinal));
  }

  // --- Ctrl-K contextual rows ---------------------------------------------------------------

  public static async Task Contribute_Rows_On_Settings_And_Passkeys_Only()
  {
    using OffersSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.AddPasskey("Work laptop", "aaaa1111");
    spa.Api.AddPasskey(nickname: null, "bbbb2222");
    spa.Api.Add(ScriptedCredentialsApiService.Active(CredentialType.EntraAccount, "Microsoft 365"));
    await OpenAsync(scope, SettingsPath);

    IReadOnlyList<CommandPaletteRow> onSettings = await PaletteRows(scope);
    onSettings.Count(static row => row.Target == OfferedActionNames.RevokeCredential).ShouldBe(3, "two passkeys and the Microsoft 365 account");
    onSettings.ShouldNotContain(static row => row.RequiresInput);
    CommandPaletteState palette = scope.Store.GetState<CommandPaletteState>();
    palette.Matches[0].Kind.ShouldBe(CommandPaletteRowKind.Contextual, "a page's own actions head the empty-query list");
    palette.Roster.ShouldContain(static row => row.Kind == CommandPaletteRowKind.Page, "the static roster is still there");

    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo(PasskeysPath);
    (await PaletteRows(scope)).Count(static row => row.Target == OfferedActionNames.RevokeCredential).ShouldBe(2, "Passkeys lists passkeys only");

    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Counter");
    (await PaletteRows(scope)).ShouldBeEmpty();
    Context(scope).Rows().ShouldBeEmpty();

    // A row copied on Settings cannot be run from another page.
    await RunAsync(scope, onSettings.First(static row => row.Target == OfferedActionNames.RevokeCredential));
    spa.Api.Requests.OfType<RevokeCredential.Command>().ShouldBeEmpty();
  }

  public static async Task Offer_Link_Microsoft365_On_Settings_Once_And_Run_The_Real_Action()
  {
    using OffersSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.AddPasskey("Work laptop", "aaaa1111");
    spa.Api.Microsoft365Offered = true;
    await OpenAsync(scope, SettingsPath);

    CommandPaletteState palette = await OpenPaletteAsync(scope);
    CommandPaletteRow link = palette.Roster.Single(static row => row.Target == OfferedActionNames.LinkMicrosoft365);
    link.Kind.ShouldBe(CommandPaletteRowKind.Contextual, "the offered row replaces the static command on this page");
    await RunAsync(scope, link);

    scope.ServiceProvider.GetRequiredService<NavigationManager>().Uri
      .ShouldBe("http://localhost/api/identity/entra/challenge?mode=link&returnUrl=%2FSettings");

    // Passkeys does not own the page-level offer: the static command is all that is left there.
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo(PasskeysPath);
    (await OpenPaletteAsync(scope)).Roster.Single(static row => row.Target == OfferedActionNames.LinkMicrosoft365)
      .Kind.ShouldBe(CommandPaletteRowKind.Command);
  }

  // --- helpers ------------------------------------------------------------------------------

  private const string SettingsPath = "/Settings";
  private const string PasskeysPath = "/Passkeys";

  private static async Task OpenAsync(SpaTestScope scope, string path)
  {
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo(path);
    await scope.Send(new CredentialsState.FetchCredentialsActionSet.Action());
  }

  private static async Task<CommandPaletteState> OpenPaletteAsync(SpaTestScope scope)
  {
    await scope.Send(new CommandPaletteState.OpenActionSet.Action());
    return scope.Store.GetState<CommandPaletteState>();
  }

  /// <summary>Contextual rows of a freshly opened palette (Open is what Ctrl-K does).</summary>
  private static async Task<IReadOnlyList<CommandPaletteRow>> PaletteRows(SpaTestScope scope) =>
    [.. (await OpenPaletteAsync(scope)).Roster.Where(static row => row.Kind == CommandPaletteRowKind.Contextual)];

  private static Task RunAsync(SpaTestScope scope, CommandPaletteRow row) =>
    CommandPaletteRunner.RunAsync(row, scope.Store, scope.ServiceProvider.GetRequiredService<IActionCatalog>(), CancellationToken.None, Context(scope));

  private static Task RunOfferAsync(SpaTestScope scope, string name, Guid? credentialId, IReadOnlyDictionary<string, JsonElement>? input) =>
    CredentialOfferRows.RunAsync(name, credentialId, input, scope.Store, scope.ServiceProvider.GetRequiredService<IActionCatalog>(), Context(scope), CancellationToken.None);

  private static IActionCatalog Catalog(OffersSpa spa) => spa.ServiceProvider.GetRequiredService<IActionCatalog>();

  private static CommandPaletteContext Context(SpaTestScope scope) =>
    scope.ServiceProvider.GetRequiredService<CommandPaletteContext>();

  private static string[] Messages(SpaTestScope scope) =>
    [.. scope.Store.GetState<NotificationState>().Messages.Select(static message => message.Title)];

  private static string[] Warnings(SpaTestScope scope) =>
    [.. scope.Store.GetState<NotificationState>().Messages.Where(static message => message.Intent == MessageBarIntent.Warning).Select(static message => message.Title)];

  /// <summary>In-proc SPA with a signed-in principal, the real catalog/palette context, and the scripted credentials BFF.</summary>
  private sealed class OffersSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public OffersApi Api { get; } = new();

    public OffersSpa() : this(PermissionIds.All) { }

    public OffersSpa(IEnumerable<string> permissions)
    {
      ClaimsPrincipal user = new(new ClaimsIdentity(
        [
          new Claim("sub", Guid.NewGuid().ToString()),
          .. permissions.Select(static permission => new Claim(PermissionIds.ClaimType, permission)),
        ],
        authenticationType: "test"));

      ServiceCollection services = new();
      services.AddLogging();
      services.AddFluentUIComponents();
      services.AddWebSpaGeneratedMediator();
      services.AddTimeWarpState
      (
        options =>
        {
          options.Assemblies =
          [
            typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly
          ];
        }
      );
      services.AddActionCatalog(typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly);
      services.AddScoped<CommandPaletteContext>();
      services.AddScoped<ICommandPaletteContextSource, CredentialsContextSource>();
      services.AddScoped<
        TimeWarp.Features.Persistence.IPersistenceService,
        TimeWarp.Features.Persistence.PersistenceService>();
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(user));
      services.AddSingleton<TimeWarp.Architecture.Services.IWebServerApiService>(Api.Scripted);
      services.AddScoped(_ => A.Fake<IJSRuntime>());
      services.AddScoped<NavigationManager, TestNavigationManager>();

      ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
      if (ServiceProvider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }

  /// <summary>Seeding helpers over the shared scripted BFF.</summary>
  private sealed class OffersApi
  {
    public ScriptedCredentialsApiService Scripted { get; } = new();
    public List<CredentialSummary> Credentials => Scripted.Credentials;
    public List<IApiRequest> Requests => Scripted.Requests;
    public List<OfferedAction> ExtraOffers => Scripted.ExtraOffers;

    public bool Microsoft365Offered
    {
      get => Scripted.Microsoft365Offered;
      set => Scripted.Microsoft365Offered = value;
    }

    public CredentialSummary Add(CredentialSummary credential)
    {
      Credentials.Add(credential);
      return credential;
    }

    public CredentialSummary AddPasskey(string? nickname, string fingerprint) =>
      Add(new CredentialSummary(CredentialId.New(), CredentialType.Passkey, "1Password", nickname, DateTimeOffset.UtcNow.AddDays(-1), revokedAt: null, isActive: true, RegisteredWith.Unknown, fingerprint));
  }

  private sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(user));
  }
}
