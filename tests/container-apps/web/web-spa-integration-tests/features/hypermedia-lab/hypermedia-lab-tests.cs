#region Purpose
// Hypermedia lab (task 275), client half: approach B (catalog actions + JSON → args binder) and
// approach C (FollowCommand link interpreter) run only what the latest payload offers, refresh to the
// server's follow-up set, fail closed on unknown names / bad arguments / foreign hrefs / stale rows,
// and contribute Ctrl-K contextual rows only while the lab page is current.
#endregion

#region Design
// C-create in-proc SPA ServiceProvider per test (AGENTS.md default): real TimeWarp.State pipeline,
// real IActionCatalog, real CommandPaletteContext + HypermediaLabContextSource, a TestNavigationManager
// for the current path, and a scripted IWebServerApiService (ScriptedLabApi) standing in for the BFF.
// The script applies the server rule the lab endpoints use (Revoke while more than one credential is
// active) and the real effects (revoke/rename), so "the follow-up payload changes the set" is
// observed end to end on the client. The rule itself, against the real handlers, is pinned in
// web-server-integration-tests (hypermedia-lab-endpoint-tests) — this file proves the client runs
// what it is given and nothing else. Runs go through the same entry points the page and the palette
// use: CommandPaletteRunner.RunAsync (palette) and RunContextualAsync (page buttons with input).
#endregion

namespace HypermediaLab_;

using System.Security.Claims;
using FakeItEasy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.HypermediaLab;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Foundation.Features;
using TimeWarp.Identity;
using TimeWarp.Architecture.Web.Spa;
using static TimeWarp.Architecture.Features.HypermediaLab.GetCredentialCommands;
using static TimeWarp.Architecture.Features.HypermediaLab.GetCredentialOffers;
using CredentialSummary = TimeWarp.Architecture.Features.Identity.GetCredentials.CredentialSummary;

[TestTag("Integration")]
public class HypermediaLab_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<HypermediaLab_Should_>();

  // --- vocabulary ---------------------------------------------------------------------------

  public static Task Name_Only_Catalog_Entries_That_Exist()
  {
    using LabSpa spa = new();
    IActionCatalog catalog = spa.ServiceProvider.GetRequiredService<IActionCatalog>();

    foreach (string name in OfferedActionNames.All)
    {
      catalog.Find(name).ShouldNotBeNull($"the server may offer {name}");
    }

    foreach (string name in new[] { OfferedActionNames.RevokeCredential, OfferedActionNames.RenameCredential })
    {
      catalog.Find(name)!.Parameters.Select(static parameter => parameter.Name).ShouldContain(OfferedActionNames.CredentialIdArgument);
    }

    catalog.Find(HypermediaLabState.FetchCredentialOffersActionSet.CatalogName).ShouldNotBeNull().Parameters.ShouldBeEmpty();
    catalog.Find(HypermediaLabState.FollowCommandActionSet.CatalogName).ShouldNotBeNull()
      .Parameters.Select(static parameter => parameter.Name)
      .ShouldBe([HypermediaLabRows.FollowCommandMethodArgument, HypermediaLabRows.FollowCommandHrefArgument, HypermediaLabRows.FollowCommandFieldsArgument]);
    return Task.CompletedTask;
  }

  public static Task Register_The_Lab_Page_With_The_Route_The_Context_Source_Matches()
  {
    PageRegistryEntry entry = PageRegistry.All.Single(static page => page.PageType == typeof(HypermediaLabPage));
    entry.RouteTemplate.ShouldBe(HypermediaLabPage.Route);
    entry.Policy.ShouldBe(PermissionIds.CredentialManageSelf);
    return Task.CompletedTask;
  }

  // --- approach B ---------------------------------------------------------------------------

  public static async Task B_Run_The_Offered_Revoke_And_Refresh_To_The_Follow_Up_Set()
  {
    using LabSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    (CredentialSummary first, CredentialSummary _) = spa.Api.SeedTwo();
    await OpenLabAsync(scope);

    HypermediaLabState lab = scope.Store.GetState<HypermediaLabState>();
    lab.Offers.ShouldNotBeNull().Offers.Count(static offer => offer.Name == OfferedActionNames.RevokeCredential).ShouldBe(2);

    CommandPaletteRow revokeFirst = (await PaletteRows(scope)).Single(row =>
      row.Name.StartsWith("Lab B: Revoke", StringComparison.Ordinal) && row.Name.Contains(first.Fingerprint, StringComparison.Ordinal));
    await RunAsync(scope, revokeFirst);

    spa.Api.Requests.OfType<RevokeCredential.Command>().Single().CredentialId.ShouldBe(first.Id.Value);
    // The runner ran the row's follow-up: the server now offers no Revoke (one credential left).
    lab = scope.Store.GetState<HypermediaLabState>();
    lab.Offers.ShouldNotBeNull().Credentials.Count.ShouldBe(1);
    lab.Offers.Offers.ShouldNotContain(static offer => offer.Name == OfferedActionNames.RevokeCredential);
    (await PaletteRows(scope)).ShouldNotContain(static row => row.Name.StartsWith("Lab B: Revoke", StringComparison.Ordinal));
  }

  public static async Task B_Rename_With_User_Input_For_The_Unbound_Parameter()
  {
    using LabSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    (CredentialSummary first, CredentialSummary _) = spa.Api.SeedTwo();
    await OpenLabAsync(scope);
    GetCredentialOffers.Response offers = scope.Store.GetState<HypermediaLabState>().Offers.ShouldNotBeNull();
    OfferedAction rename = offers.Offers.Single(offer => offer.Name == OfferedActionNames.RenameCredential && offer.Subject == first.Id.Value.ToString("D"));
    IActionCatalog catalog = spa.ServiceProvider.GetRequiredService<IActionCatalog>();

    HypermediaLabRows.UnboundParameters(catalog.Find(rename.Name), rename).ShouldBe(["nickname"]);
    CommandPaletteRow row = HypermediaLabRows.OfferRow(offers, rename, catalog);
    row.RequiresInput.ShouldBeTrue();
    (await PaletteRows(scope)).ShouldNotContain(row, "the palette has no argument UI");

    await CommandPaletteRunner.RunContextualAsync(row, Input("nickname", "Desk key"), scope.Store, catalog, Context(scope), CancellationToken.None);

    spa.Api.Requests.OfType<RenameCredential.Command>().Single().Nickname.ShouldBe("Desk key");
    scope.Store.GetState<HypermediaLabState>().Offers!.Credentials
      .Single(credential => credential.Id == first.Id).Nickname.ShouldBe("Desk key");
  }

  public static async Task B_Refuse_Input_That_Overrides_An_Offered_Argument()
  {
    using LabSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    (CredentialSummary first, CredentialSummary second) = spa.Api.SeedTwo();
    await OpenLabAsync(scope);
    GetCredentialOffers.Response offers = scope.Store.GetState<HypermediaLabState>().Offers!;
    OfferedAction rename = offers.Offers.Single(offer => offer.Name == OfferedActionNames.RenameCredential && offer.Subject == first.Id.Value.ToString("D"));
    IActionCatalog catalog = spa.ServiceProvider.GetRequiredService<IActionCatalog>();
    Dictionary<string, JsonElement> input = new()
    {
      ["nickname"] = JsonSerializer.SerializeToElement("x"),
      [OfferedActionNames.CredentialIdArgument] = JsonSerializer.SerializeToElement(second.Id.Value),
    };

    await CommandPaletteRunner.RunContextualAsync(HypermediaLabRows.OfferRow(offers, rename, catalog), input, scope.Store, catalog, Context(scope), CancellationToken.None);

    spa.Api.Requests.OfType<RenameCredential.Command>().ShouldBeEmpty();
    Warnings(scope).ShouldContain(static title => title.Contains("cannot replace the offered 'credentialId'", StringComparison.Ordinal));
  }

  public static async Task B_Fail_Closed_On_An_Unknown_Catalog_Name()
  {
    using LabSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.SeedTwo();
    spa.Api.ExtraOffers.Add(new OfferedAction("Credentials.DeleteEverything", "Delete everything", subject: null, new Dictionary<string, JsonElement>()));
    await OpenLabAsync(scope);

    CommandPaletteRow unknown = (await PaletteRows(scope)).Single(static row => row.Target == "Credentials.DeleteEverything");
    int before = spa.Api.Requests.Count;
    await RunAsync(scope, unknown);

    spa.Api.Requests.Count.ShouldBe(before, "nothing is sent, not even the follow-up");
    Warnings(scope).ShouldContain(static title => title.Contains("is not an action this app knows", StringComparison.Ordinal));
  }

  [Input("{\"credentialId\":\"not-a-guid\"}")]
  [Input("{\"credentialId\":\"6f1c1f43-3f4e-4f43-9f43-5f1c1f433f4e\",\"extra\":1}")]
  [Input("{}")]
  [Input("[1,2]")]
  public static async Task B_Fail_Closed_On_Arguments_That_Do_Not_Bind(string argumentsJson)
  {
    using LabSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.SeedTwo();
    spa.Api.ExtraOffersJson.Add(argumentsJson);
    await OpenLabAsync(scope);

    IActionCatalog catalog = spa.ServiceProvider.GetRequiredService<IActionCatalog>();
    // From the contributed set (not the palette): an offer leaving credentialId unbound is a button-only row.
    CommandPaletteRow bad = HypermediaLabRows.All(scope.Store.GetState<HypermediaLabState>(), catalog).Single(static row => row.Name == "Lab B: Bad revoke");
    await CommandPaletteRunner.RunContextualAsync(bad, input: null, scope.Store, catalog, Context(scope), CancellationToken.None);

    spa.Api.Requests.OfType<RevokeCredential.Command>().ShouldBeEmpty();
    Warnings(scope).ShouldContain(static title => title.StartsWith("Lab B: Bad revoke was refused:", StringComparison.Ordinal));
  }

  public static async Task B_Refuse_A_Row_The_Current_Payload_Does_Not_Offer()
  {
    using LabSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.SeedOne();
    await OpenLabAsync(scope);
    CredentialSummary only = spa.Api.Credentials.Single();
    // Revoke is not offered for the last credential; a hand-built row naming it must not run.
    CommandPaletteRow forged = new
    (
      "Lab B: Revoke",
      "",
      CommandPaletteRowKind.Contextual,
      OfferedActionNames.RevokeCredential,
      $"{{\"credentialId\":\"{only.Id.Value:D}\"}}",
      HypermediaLabState.FetchCredentialOffersActionSet.CatalogName
    );

    await RunAsync(scope, forged);

    spa.Api.Requests.OfType<RevokeCredential.Command>().ShouldBeEmpty();
    Warnings(scope).ShouldContain("Lab B: Revoke is not offered here now.");
  }

  // --- approach C ---------------------------------------------------------------------------

  public static async Task C_Follow_The_Offered_Revoke_Link_And_Refresh_From_Self()
  {
    using LabSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    (CredentialSummary first, CredentialSummary _) = spa.Api.SeedTwo();
    await OpenLabAsync(scope);
    scope.Store.GetState<HypermediaLabState>().Commands.ShouldNotBeNull()
      .Commands.Count(static command => command.Rel == LinkCommandRels.Revoke).ShouldBe(2);

    CommandPaletteRow revokeFirst = (await PaletteRows(scope)).Single(row =>
      row.Name.StartsWith("Lab C: Revoke", StringComparison.Ordinal) && row.Name.Contains(first.Fingerprint, StringComparison.Ordinal));
    await RunAsync(scope, revokeFirst);

    FollowedLinkRequest post = spa.Api.Requests.OfType<FollowedLinkRequest>().Single(static request => request.Verb == HttpVerb.Post);
    post.Href.ShouldBe($"/api/identity/credentials/{first.Id.Value:D}/revoke");
    post.Body[LinkCommandRels.UserIdField].GetGuid().ShouldBe(spa.Api.UserId);
    JsonSerializer.Serialize(post, ContractSerializationDefaults.Options).ShouldBe($"{{\"userId\":\"{spa.Api.UserId:D}\"}}");
    spa.Api.Requests.OfType<FollowedLinkRequest>().ShouldContain(static request => request.Verb == HttpVerb.Get && request.Href == SelfHref);

    GetCredentialCommands.Response commands = scope.Store.GetState<HypermediaLabState>().Commands.ShouldNotBeNull();
    commands.Credentials.Count.ShouldBe(1);
    commands.Commands.ShouldNotContain(static command => command.Rel == LinkCommandRels.Revoke);
    Messages(scope).ShouldContain("Revoke: done.");
  }

  public static async Task C_Send_The_Users_Fields_With_The_Body_Template()
  {
    using LabSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    (CredentialSummary first, CredentialSummary _) = spa.Api.SeedTwo();
    await OpenLabAsync(scope);
    GetCredentialCommands.Response commands = scope.Store.GetState<HypermediaLabState>().Commands!;
    LinkCommand rename = commands.Commands.Single(command => command.Rel == LinkCommandRels.Rename && command.Subject == first.Id.Value.ToString("D"));
    CommandPaletteRow row = HypermediaLabRows.CommandRow(commands, rename);
    row.RequiresInput.ShouldBeTrue();

    Dictionary<string, JsonElement> input = new()
    {
      [HypermediaLabRows.FollowCommandFieldsArgument] = JsonSerializer.SerializeToElement(new Dictionary<string, string> { ["nickname"] = "Desk key" }),
    };
    await CommandPaletteRunner.RunContextualAsync(row, input, scope.Store, spa.ServiceProvider.GetRequiredService<IActionCatalog>(), Context(scope), CancellationToken.None);

    FollowedLinkRequest post = spa.Api.Requests.OfType<FollowedLinkRequest>().Single(static request => request.Verb == HttpVerb.Post);
    post.Body["nickname"].GetString().ShouldBe("Desk key");
    post.Body[LinkCommandRels.UserIdField].GetGuid().ShouldBe(spa.Api.UserId);
    scope.Store.GetState<HypermediaLabState>().Commands!.Credentials.Single(credential => credential.Id == first.Id).Nickname.ShouldBe("Desk key");
  }

  [Input("https://evil.example/api/identity/credentials/x/revoke")]
  [Input("//evil.example/api/x")]
  [Input("/\\evil.example/api/x")]
  [Input("javascript:alert(1)")]
  [Input("api/identity/credentials")]
  [Input("/api/x y")]
  public static async Task C_Refuse_An_Href_That_Is_Not_App_Relative(string href)
  {
    AppRelativeHref.IsAppRelative(href).ShouldBeFalse();

    using LabSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.SeedTwo();
    spa.Api.ExtraCommands.Add(new LinkCommand("evil", "Evil", subject: null, LinkCommandMethods.Post, href, body: null, []));
    await OpenLabAsync(scope);
    int before = spa.Api.Requests.Count;

    await scope.Send(new HypermediaLabState.FollowCommandActionSet.Action(LinkCommandMethods.Post, href, new Dictionary<string, string>()));

    spa.Api.Requests.Count.ShouldBe(before);
    Warnings(scope).ShouldContain(title => title == $"Refused: '{href}' is not an app-relative link.");
  }

  public static Task Accept_App_Relative_Hrefs()
  {
    AppRelativeHref.IsAppRelative("/api/identity/credentials/1/revoke").ShouldBeTrue();
    AppRelativeHref.IsAppRelative("/api/identity/entra/challenge?mode=link&returnUrl=%2FHypermediaLab").ShouldBeTrue();
    AppRelativeHref.IsAppRelative(null).ShouldBeFalse();
    AppRelativeHref.IsAppRelative("").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static async Task C_Refuse_A_Command_The_Current_Payload_Does_Not_Offer()
  {
    using LabSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.SeedOne();
    await OpenLabAsync(scope);
    CredentialSummary only = spa.Api.Credentials.Single();
    int before = spa.Api.Requests.Count;

    await scope.Send(new HypermediaLabState.FollowCommandActionSet.Action(
      LinkCommandMethods.Post, $"/api/identity/credentials/{only.Id.Value:D}/revoke", new Dictionary<string, string>()));

    spa.Api.Requests.Count.ShouldBe(before);
    Warnings(scope).ShouldContain("That command is not offered now.");
  }

  public static async Task C_Navigate_For_Link_Microsoft_365()
  {
    using LabSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.SeedOne();
    spa.Api.Microsoft365Offered = true;
    await OpenLabAsync(scope);

    CommandPaletteRow link = (await PaletteRows(scope)).Single(static row => row.Name == "Lab C: Link Microsoft 365");
    await RunAsync(scope, link);

    scope.ServiceProvider.GetRequiredService<NavigationManager>().Uri
      .ShouldBe("http://localhost/api/identity/entra/challenge?mode=link&returnUrl=%2FHypermediaLab");
    spa.Api.Requests.OfType<FollowedLinkRequest>().ShouldBeEmpty();
  }

  // --- Ctrl-K hook --------------------------------------------------------------------------

  public static async Task Contribute_Palette_Rows_On_The_Lab_Page_And_None_Elsewhere()
  {
    using LabSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.SeedTwo();
    await OpenLabAsync(scope);

    IReadOnlyList<CommandPaletteRow> onLab = await PaletteRows(scope);
    onLab.Count(static row => row.Name.StartsWith("Lab B: Revoke", StringComparison.Ordinal)).ShouldBe(2);
    onLab.Count(static row => row.Name.StartsWith("Lab C: Revoke", StringComparison.Ordinal)).ShouldBe(2);
    onLab.ShouldNotContain(static row => row.RequiresInput);
    CommandPaletteState palette = scope.Store.GetState<CommandPaletteState>();
    palette.Matches[0].Kind.ShouldBe(CommandPaletteRowKind.Contextual, "a page's own actions head the empty-query list");
    palette.Roster.ShouldContain(static row => row.Kind == CommandPaletteRowKind.Page, "the static roster is still there");

    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Counter");
    await scope.Send(new CommandPaletteState.OpenActionSet.Action());

    scope.Store.GetState<CommandPaletteState>().Roster.ShouldNotContain(static row => row.Kind == CommandPaletteRowKind.Contextual);
    Context(scope).Rows().ShouldBeEmpty();

    // A row copied while on the lab page cannot be run from another page.
    CommandPaletteRow stale = onLab.First(static row => row.Name.StartsWith("Lab B: Revoke", StringComparison.Ordinal));
    await RunAsync(scope, stale);
    spa.Api.Requests.OfType<RevokeCredential.Command>().ShouldBeEmpty();
  }

  // --- helpers ------------------------------------------------------------------------------

  private static async Task OpenLabAsync(SpaTestScope scope)
  {
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo(HypermediaLabPage.Route);
    await scope.Send(new HypermediaLabState.FetchCredentialOffersActionSet.Action());
    await scope.Send(new HypermediaLabState.FetchCredentialCommandsActionSet.Action());
  }

  /// <summary>Contextual rows of a freshly opened palette (Open is what Ctrl-K does).</summary>
  private static async Task<IReadOnlyList<CommandPaletteRow>> PaletteRows(SpaTestScope scope)
  {
    await scope.Send(new CommandPaletteState.OpenActionSet.Action());
    return [.. scope.Store.GetState<CommandPaletteState>().Roster.Where(static row => row.Kind == CommandPaletteRowKind.Contextual)];
  }

  private static Task RunAsync(SpaTestScope scope, CommandPaletteRow row) =>
    CommandPaletteRunner.RunAsync(row, scope.Store, scope.ServiceProvider.GetRequiredService<IActionCatalog>(), CancellationToken.None, Context(scope));

  private static CommandPaletteContext Context(SpaTestScope scope) =>
    scope.ServiceProvider.GetRequiredService<CommandPaletteContext>();

  private static Dictionary<string, JsonElement> Input(string name, string value) =>
    new() { [name] = JsonSerializer.SerializeToElement(value) };

  private static string[] Messages(SpaTestScope scope) =>
    [.. scope.Store.GetState<NotificationState>().Messages.Select(static message => message.Title)];

  private static string[] Warnings(SpaTestScope scope) =>
    [.. scope.Store.GetState<NotificationState>().Messages.Where(static message => message.Intent == MessageBarIntent.Warning).Select(static message => message.Title)];

  /// <summary>In-proc SPA with a signed-in principal, the real catalog/palette context, and the scripted lab BFF.</summary>
  private sealed class LabSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public ScriptedLabApi Api { get; } = new();

    public LabSpa()
    {
      ClaimsPrincipal user = new(new ClaimsIdentity(
        [
          new Claim("sub", Api.UserId.ToString()),
          .. PermissionIds.All.Select(static permission => new Claim(PermissionIds.ClaimType, permission)),
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
      services.AddScoped<ICommandPaletteContextSource, HypermediaLabContextSource>();
      services.AddScoped<
        TimeWarp.Features.Persistence.IPersistenceService,
        TimeWarp.Features.Persistence.PersistenceService>();
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(user));
      services.AddSingleton<TimeWarp.Architecture.Services.IWebServerApiService>(Api);
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

  private sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(user));
  }

  /// <summary>
  /// Scripted BFF for the lab: answers both lab reads with the server's rule (Revoke while more than
  /// one credential is active; Link while Microsoft 365 is offered and none is linked) and applies
  /// revoke / rename from either the typed commands (B) or the followed links (C).
  /// </summary>
  private sealed class ScriptedLabApi : TimeWarp.Architecture.Services.IWebServerApiService
  {
    public Guid UserId { get; } = Guid.NewGuid();
    public List<CredentialSummary> Credentials { get; } = [];
    public List<IApiRequest> Requests { get; } = [];
    public List<OfferedAction> ExtraOffers { get; } = [];
    public List<string> ExtraOffersJson { get; } = [];
    public List<LinkCommand> ExtraCommands { get; } = [];
    public bool Microsoft365Offered { get; set; }

    public (CredentialSummary First, CredentialSummary Second) SeedTwo()
    {
      Credentials.Add(Passkey("Work laptop", "aaaa1111"));
      Credentials.Add(Passkey(nickname: null, "bbbb2222"));
      return (Credentials[0], Credentials[1]);
    }

    public void SeedOne() => Credentials.Add(Passkey("Work laptop", "aaaa1111"));

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      _ = cancellationToken;
      Requests.Add(request);
      object? response = request switch
      {
        GetCredentialOffers.Query => Offers(),
        GetCredentialCommands.Query => Commands(),
        FollowedLinkRequest { Verb: HttpVerb.Get } get when get.Href == SelfHref => Commands(),
        FollowedLinkRequest { Verb: HttpVerb.Post } post => Follow(post),
        RevokeCredential.Command revoke => Revoke(revoke.CredentialId),
        RenameCredential.Command rename => Rename(rename.CredentialId, rename.Nickname),
        _ => throw new InvalidOperationException($"No scripted response for {request.GetType()} → {typeof(TResponse)}.")
      };

      return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(response switch
      {
        SharedProblemDetails problem => problem,
        TResponse typed => typed,
        // FollowCommand reads the POST body as an internal placeholder type; any instance will do.
        _ => (TResponse)Activator.CreateInstance(typeof(TResponse), nonPublic: true)!
      });
    }

    private GetCredentialOffers.Response Offers()
    {
      List<OfferedAction> offers = [];
      foreach (CredentialSummary credential in Credentials)
      {
        offers.Add(ForCredential(OfferedActionNames.RenameCredential, "Rename", credential.Id));
        if (Credentials.Count > 1)
        {
          offers.Add(ForCredential(OfferedActionNames.RevokeCredential, "Revoke", credential.Id));
        }
      }

      if (Microsoft365Offered)
      {
        offers.Add(new OfferedAction(OfferedActionNames.LinkMicrosoft365, "Link Microsoft 365", subject: null, new Dictionary<string, JsonElement>()));
      }

      offers.AddRange(ExtraOffers);
      foreach (string json in ExtraOffersJson)
      {
        offers.Add(new OfferedAction(OfferedActionNames.RevokeCredential, "Bad revoke", subject: null,
          JsonDocument.Parse(json).RootElement.ValueKind == JsonValueKind.Object
            ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!
            : new Dictionary<string, JsonElement> { ["credentialId"] = JsonDocument.Parse(json).RootElement.Clone() }));
      }

      return new GetCredentialOffers.Response([.. Credentials], offers);
    }

    private GetCredentialCommands.Response Commands()
    {
      List<LinkCommand> commands = [];
      foreach (CredentialSummary credential in Credentials)
      {
        commands.Add(GetCredentialCommands.Rename(credential.Id, UserId));
        if (Credentials.Count > 1)
        {
          commands.Add(GetCredentialCommands.Revoke(credential.Id, UserId));
        }
      }

      if (Microsoft365Offered)
      {
        commands.Add(GetCredentialCommands.LinkMicrosoft365("/HypermediaLab"));
      }

      commands.AddRange(ExtraCommands);
      return new GetCredentialCommands.Response([.. Credentials], commands, SelfHref);
    }

    private object Follow(FollowedLinkRequest post)
    {
      foreach (CredentialSummary credential in Credentials.ToList())
      {
        string id = credential.Id.Value.ToString("D");
        if (post.Href == $"/api/identity/credentials/{id}/revoke")
        {
          return Revoke(credential.Id.Value);
        }

        if (post.Href == $"/api/identity/credentials/{id}/rename")
        {
          return Rename(credential.Id.Value, post.Body["nickname"].GetString()!);
        }
      }

      return new SharedProblemDetails { Title = "Not found", Status = 404 };
    }

    private object Revoke(Guid credentialId)
    {
      if (Credentials.Count <= 1)
      {
        return new SharedProblemDetails { Title = "Cannot revoke last credential", Status = 409 };
      }

      Credentials.RemoveAll(credential => credential.Id.Value == credentialId);
      return new RevokeCredential.Response();
    }

    private object Rename(Guid credentialId, string nickname)
    {
      int index = Credentials.FindIndex(credential => credential.Id.Value == credentialId);
      CredentialSummary row = Credentials[index];
      Credentials[index] = new CredentialSummary(row.Id, row.Type, row.Label, nickname.Trim(), row.CreatedAt, row.RevokedAt, row.IsActive, row.RegisteredWith, row.Fingerprint);
      return new RenameCredential.Response();
    }

    private static CredentialSummary Passkey(string? nickname, string fingerprint) =>
      new(CredentialId.New(), CredentialType.Passkey, "1Password", nickname, DateTimeOffset.UtcNow.AddDays(-1), revokedAt: null, isActive: true, RegisteredWith.Unknown, fingerprint);
  }
}
