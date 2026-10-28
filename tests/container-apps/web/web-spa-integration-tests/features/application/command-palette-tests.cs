#region Purpose
// Ctrl-K command palette (task 239-003): roster from PageRegistry + the generated ActionRoster (task 301),
// permission filter, ranking order, keyboard highlight, Enter navigating / executing, and the overlay open/close render;
// plus the signed-out Sign in row (task 259), authored [CatalogAction(DisplayName)] labels with the generated
// fallback (task 268), and derived commands with explicit [CommandPaletteOptOut] and runtime label overrides (task 301).
#endregion

#region Design
// C-create in-proc SPA ServiceProvider per test (no Aspire): the palette never calls a server, and
// the permission filter needs a controllable principal. Authorization is the SPA's own
// PolicyRegistration (claim policies keyed by PermissionIds), so the rows a principal sees are
// decided by the same policies NavMenu filters its pages with (NavigationDestinations). The principal is a fixed
// AuthenticationStateProvider carrying PermissionIds.ClaimType claims.
// Keyboard behaviour is pinned at the state seam the component binds to (Filter / MoveHighlight /
// Highlighted, then CommandPaletteRunner — what Enter does): there is no bUnit and the JS hotkey
// needs a browser. Open/close is a static HtmlRenderer render of ModalController with and without
// ActiveModalId == CommandPalette.ModalId. "Enter executes a parameterless command" runs a probe
// ActionRosterEntry through the runner so the test observes exactly one Execute.
// The command set is not a hand list (task 301): it is asserted against ActionRoster.All — every
// action that is not opted out, has no required parameter and is Human-visible is a row, and every
// other is absent. The guard test fails on an opt-out without a reason, on an action that is both
// opted out and authored as a Human [CatalogAction] (two contradicting decisions), and on any
// *ActionSet.Action in web-spa (found by test-only reflection) that ActionRoster.All does not list.
// Manual browser check (Ctrl-K / Cmd-K, search-field trigger, focus return, mobile viewport) is not
// automated here — it needs a running AppHost.
#endregion

namespace CommandPalette_;

using FakeItEasy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Security.Claims;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.StyleGuide;
using TimeWarp.Architecture.Web.Spa;

[TestTag("Unit")]
public class CommandPalette_Should_
{
  private static readonly string[] Everything = [.. PermissionIds.All];

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CommandPalette_Should_>();

  public static async Task List_Permitted_Pages_And_Parameterless_Human_Commands()
  {
    using PaletteSpa spa = new(Everything);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new CommandPaletteState.OpenActionSet.Action());
    CommandPaletteState palette = scope.Store.GetState<CommandPaletteState>();

    palette.Roster.Where(static row => row.Kind == CommandPaletteRowKind.Page).Select(static row => row.Target)
      .ShouldBe(
        [
          .. PageRegistry.All.Select(static page => page.Url),
          .. StyleGuideParts.PageOrder.Select(static part => StyleGuideParts.Url(part)),
        ],
        ignoreOrder: true);

    // Derived, not hand-listed (task 301): every eligible roster action is a row, every other is absent.
    Dictionary<Type, ActionCatalogEntry> catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>().Entries
      .ToDictionary(static entry => entry.ActionType);
    string[] expected =
    [
      .. ActionRoster.All
        .Where(action => string.IsNullOrWhiteSpace(action.OptOutReason)
          && action.RequiredParameterCount == 0
          && (catalog.GetValueOrDefault(action.ActionType)?.Visibility ?? ActionVisibility.Human).HasFlag(ActionVisibility.Human))
        .Select(static action => action.Name)
    ];
    palette.Roster.Where(static row => row.Kind == CommandPaletteRowKind.Command).Select(static row => row.Target)
      .ShouldBe(expected, ignoreOrder: true);
    expected.ShouldContain("AgentSurface.OpenAskPanel");
    expected.ShouldContain("Profile.SignOut");

    // Task 260: the Settings "Link Microsoft 365" button is a palette command too; task 268: its
    // label is the authored [CatalogAction(DisplayName)], so the brand casing survives.
    palette.Roster.Single(static row => row.Target == "Credentials.LinkMicrosoft365").Name.ShouldBe("Credentials: Link Microsoft 365");

    CommandPaletteRow signOut = palette.Roster.Single(static row => row.Target == "Profile.SignOut");
    signOut.Name.ShouldBe("Profile: Sign out");
    signOut.Description.ShouldBe("Sign the current user out of this browser session.");

    // Signed in: no Sign in row, and Login is still not a registry destination (NavMenu must not list it).
    palette.Roster.ShouldNotContain(static row => row.Target.StartsWith(LoginPage.GetPageUrl(), StringComparison.OrdinalIgnoreCase));
    PageRegistry.All.ShouldNotContain(static page => page.Url == LoginPage.GetPageUrl());

    // Empty query: every row, pages first, highlight on the first.
    palette.Matches.Count.ShouldBe(palette.Roster.Count);
    palette.Matches[0].Kind.ShouldBe(CommandPaletteRowKind.Page);
    palette.Matches[^1].Kind.ShouldBe(CommandPaletteRowKind.Command);
    palette.HighlightedIndex.ShouldBe(0);
    palette.Roster.Count(static row => row.Target == "/StyleGuide").ShouldBe(1);
    CommandPaletteRow colorTokens = palette.Roster.Single(static row => row.Target == "/StyleGuide/color-tokens");
    colorTokens.Name.ShouldBe("Style guide: Color Tokens");
    colorTokens.Kind.ShouldBe(CommandPaletteRowKind.Page);
  }

  public static Task Opt_Out_Only_Explicitly_With_A_Reason()
  {
    using PaletteSpa spa = new(Everything);
    Dictionary<Type, ActionCatalogEntry> catalog = spa.ServiceProvider.GetRequiredService<IActionCatalog>().Entries
      .ToDictionary(static entry => entry.ActionType);

    ActionRoster.All.ShouldNotBeEmpty();
    foreach (ActionRosterEntry action in ActionRoster.All)
    {
      if (action.OptOutReason is null)
      {
        continue;
      }

      action.OptOutReason.ShouldNotBeNullOrWhiteSpace($"{action.Name}: [CommandPaletteOptOut] needs a reason.");
      ActionCatalogEntry? metadata = catalog.GetValueOrDefault(action.ActionType);
      if (metadata is not null)
      {
        metadata.Visibility.HasFlag(ActionVisibility.Human)
          .ShouldBeFalse($"{action.Name} is a Human [CatalogAction] and also opted out of the palette.");
      }
    }

    // The roster is complete: every *ActionSet.Action in web-spa that is a TimeWarp.Mediator.IAction is
    // listed, so no action can leave the palette by being silently skipped by the generator.
    HashSet<Type> listed = [.. ActionRoster.All.Select(static action => action.ActionType)];
    Type[] declared = [.. typeof(ActionRoster).Assembly.GetTypes()
      .Where(static type => type.Name == "Action"
        && type.DeclaringType is { } actionSet
        && actionSet.Name.EndsWith("ActionSet", StringComparison.Ordinal)
        && typeof(TimeWarp.Mediator.IAction).IsAssignableFrom(type))];
    declared.ShouldNotBeEmpty();
    foreach (Type action in declared)
    {
      listed.ShouldContain(action, $"{action.FullName} is an ActionSet action missing from ActionRoster.All.");
    }

    return Task.CompletedTask;
  }

  public static async Task List_Open_Ask_Panel_With_No_Attribute_And_Open_The_Panel()
  {
    using PaletteSpa spa = new(Everything);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    ActionRosterEntry openAsk = ActionRoster.All.Single(static action => action.Name == "AgentSurface.OpenAskPanel");
    openAsk.OptOutReason.ShouldBeNull();
    scope.ServiceProvider.GetRequiredService<IActionCatalog>().Entries
      .ShouldNotContain(static entry => entry.ActionType == typeof(AgentSurfaceState.OpenAskPanelActionSet.Action));

    await scope.Send(new CommandPaletteState.OpenActionSet.Action());
    await scope.Send(new CommandPaletteState.FilterActionSet.Action("ask"));
    CommandPaletteRow highlighted = scope.Store.GetState<CommandPaletteState>().Highlighted.ShouldNotBeNull();
    highlighted.Target.ShouldBe("AgentSurface.OpenAskPanel");
    highlighted.Name.ShouldBe("Agent surface: Open ask panel");

    await CommandPaletteRunner.RunAsync(highlighted, scope.Store, ActionRoster.All, CancellationToken.None);

    scope.Store.GetState<AgentSurfaceState>().IsPanelOpen.ShouldBeTrue();
  }

  public static async Task Apply_A_Runtime_Display_Override_Without_Changing_The_Target()
  {
    using PaletteSpa spa = new(Everything);
    IActionDisplayOverride assistantName = new AskOverride();

    IReadOnlyList<CommandPaletteRow> roster = await CommandPaletteRoster.BuildAsync(
      spa.User,
      spa.ServiceProvider.GetRequiredService<IAuthorizationService>(),
      [],
      ActionRoster.All,
      spa.ServiceProvider.GetRequiredService<IActionCatalog>().Entries,
      [assistantName],
      "/");

    CommandPaletteRow ask = roster.Single(static row => row.Target == "AgentSurface.OpenAskPanel");
    ask.Name.ShouldBe("Ask Akili");
    ask.Description.ShouldBe("Open the docked assistant panel.");
    roster.Single(static row => row.Target == "Profile.SignOut").Name.ShouldBe("Profile: Sign out");
  }

  [Input("Counter.IncrementCounter")]
  [Input("Credentials.RenameCredential")]
  [Input("Credentials.RevokeCredential")]
  [Input("Profile.UpdateProfile")]
  [Input("Role.CreateRole")]
  [Input("SiteSettings.UpdateAssistantName")]
  [Input("SiteSettings.UpdateSiteSettings")]
  [Input("SiteSettings.UpdateXaiByokMode")]
  [Input("Application.FiveSecondTask")]
  [Input("Application.TwoSecondTask")]
  [Input("Counter.ThrowException")]
  [Input("Profile.ClearProfileData")]
  [Input("Feedback.SubmitFeedback")]
  [Input("Feedback.OpenFeedback")]
  [Input("Credentials.FetchCredentials")]
  [Input("Theme.Update")]
  [Input("CommandPalette.Open")]
  [Input("Application.CloseModal")]
  [Input("AgentSurface.LoadChatConfiguration")]
  public static async Task Exclude_Agent_Parameterized_And_Internal_Actions(string name)
  {
    using PaletteSpa spa = new(Everything);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new CommandPaletteState.OpenActionSet.Action());

    scope.Store.GetState<CommandPaletteState>().Roster.ShouldNotContain(row => row.Target == name);
  }

  public static async Task Hide_Rows_The_Principal_Cannot_Run()
  {
    // A member: profile + settings read, sign-out, but no developer/admin/credential permissions.
    using PaletteSpa spa = new([PermissionIds.ProfileRead, PermissionIds.SettingsRead]);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new CommandPaletteState.OpenActionSet.Action());
    string[] targets = [.. scope.Store.GetState<CommandPaletteState>().Roster.Select(static row => row.Target)];

    targets.ShouldContain("/");
    targets.ShouldContain("/Profile");
    targets.ShouldContain("/Settings");
    targets.ShouldContain("Profile.SignOut");
    foreach (string denied in new[]
    {
      "/Counter", "/Chat", "/StyleGuide", "/Admin/Roles", "/Admin/Principals", "/Admin/Feedback", "/Admin/Authentication",
      "/Admin/AI",
      "/AgentLinks", "/Feedback", "Feedback.ListMyFeedback", "Credentials.AddPasskey", "Credentials.AddExistingPasskey", "Credentials.LinkMicrosoft365",
    })
    {
      targets.ShouldNotContain(denied);
    }

    targets.ShouldNotContain(static target => target.StartsWith("/StyleGuide/", StringComparison.OrdinalIgnoreCase));
  }

  public static async Task Offer_Anonymous_Visitors_Only_Unrestricted_Pages()
  {
    using PaletteSpa spa = new(permissions: null);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new CommandPaletteState.OpenActionSet.Action());
    CommandPaletteState palette = scope.Store.GetState<CommandPaletteState>();

    palette.Roster.ShouldAllBe(static row => row.Kind == CommandPaletteRowKind.Page);
    palette.Roster.Select(static row => row.Target)
      .ShouldBe(
        [
          .. PageRegistry.All.Where(static page => page.Policy == AuthorizationConstants.Policies.Anonymous).Select(static page => page.Url),
          LoginPage.GetPageUrl(),
        ],
        ignoreOrder: true);
    palette.Roster.Select(static row => row.Target).ShouldContain("/");

    CommandPaletteRow signIn = palette.Roster.Single(static row => row.Target == LoginPage.GetPageUrl());
    signIn.Name.ShouldBe("Sign in");
    signIn.Description.ShouldBe("Log in: go to /Login");
  }

  [Input("sign")]
  [Input("Sign in")]
  [Input("login")]
  [Input("log in")]
  public static async Task Rank_The_Sign_In_Row_First_For_Sign_In_Wording(string query)
  {
    using PaletteSpa spa = new(permissions: null);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new CommandPaletteState.OpenActionSet.Action());

    await scope.Send(new CommandPaletteState.FilterActionSet.Action(query));

    scope.Store.GetState<CommandPaletteState>().Highlighted.ShouldNotBeNull().Name.ShouldBe("Sign in");
  }

  public static async Task Rank_A_Style_Guide_Part_From_Its_Title()
  {
    using PaletteSpa spa = new(Everything);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new CommandPaletteState.OpenActionSet.Action());

    await scope.Send(new CommandPaletteState.FilterActionSet.Action("color tokens"));

    CommandPaletteRow highlighted = scope.Store.GetState<CommandPaletteState>().Highlighted.ShouldNotBeNull();
    highlighted.Name.ShouldBe("Style guide: Color Tokens");
    highlighted.Target.ShouldBe("/StyleGuide/color-tokens");
    highlighted.Kind.ShouldBe(CommandPaletteRowKind.Page);
  }

  [Input("microsoft")]
  [Input("Microsoft 365")]
  [Input("link microsoft")]
  public static async Task Rank_Link_Microsoft_365_First_For_Its_Authored_Label(string query)
  {
    using PaletteSpa spa = new(Everything);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new CommandPaletteState.OpenActionSet.Action());

    await scope.Send(new CommandPaletteState.FilterActionSet.Action(query));

    CommandPaletteRow highlighted = scope.Store.GetState<CommandPaletteState>().Highlighted.ShouldNotBeNull();
    highlighted.Target.ShouldBe("Credentials.LinkMicrosoft365");
    highlighted.Name.ShouldBe("Credentials: Link Microsoft 365");
  }

  public static async Task Rank_Link_Microsoft_365_On_Its_Label_For_Link()
  {
    using PaletteSpa spa = new(Everything);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new CommandPaletteState.OpenActionSet.Action());

    await scope.Send(new CommandPaletteState.FilterActionSet.Action("link"));

    // "link" is a word start in the label, so it ranks with the other "…link…" names, ahead of
    // every row that matches on its description alone.
    IReadOnlyList<CommandPaletteRow> matches = scope.Store.GetState<CommandPaletteState>().Matches;
    int index = matches.ToList().FindIndex(static row => row.Target == "Credentials.LinkMicrosoft365");
    index.ShouldBeGreaterThanOrEqualTo(0);
    matches.Take(index).ShouldAllBe(static row => row.Name.Contains("link", StringComparison.OrdinalIgnoreCase));
  }

  public static async Task Label_Commands_With_DisplayName_Else_The_Generated_Name()
  {
    using PaletteSpa spa = new(Everything);
    ActionRosterEntry runTwice = Probe("Probe.RunTwice", typeof(ProbeTwice));
    ActionRosterEntry runOnce = Probe("Probe.RunOnce", typeof(ProbeOnce));
    ActionRosterEntry openPanel = Probe("AgentSurface.OpenAskPanel", typeof(ProbeTwice[]));
    ActionCatalog catalog = new([new ActionCatalogSource([Metadata("Probe.RunOnce", typeof(ProbeOnce), "Run once now")])]);

    IReadOnlyList<CommandPaletteRow> roster = await CommandPaletteRoster.BuildAsync(
      spa.User, spa.ServiceProvider.GetRequiredService<IAuthorizationService>(), [], [runTwice, runOnce, openPanel], catalog.Entries, [], "/");

    roster.Single(static row => row.Target == "Probe.RunTwice").Name.ShouldBe("Probe: Run twice");
    roster.Single(static row => row.Target == "Probe.RunTwice").Description.ShouldBeEmpty();
    roster.Single(static row => row.Target == "Probe.RunOnce").Name.ShouldBe("Probe: Run once now");
    roster.Single(static row => row.Target == "Probe.RunOnce").Description.ShouldBe("Run the probe.");
    roster.Single(static row => row.Target == "AgentSurface.OpenAskPanel").Name.ShouldBe("Agent surface: Open ask panel");
  }

  public static async Task Exclude_Opted_Out_Parameterized_And_Agent_Only_Probes()
  {
    using PaletteSpa spa = new(Everything);
    ActionRosterEntry optedOut = Probe("Probe.OptedOut", typeof(ProbeOnce)) with { OptOutReason = "Plumbing." };
    ActionRosterEntry blankReason = Probe("Probe.BlankReason", typeof(ProbeTwice)) with { OptOutReason = " " };
    ActionRosterEntry needsArgument = Probe("Probe.NeedsArgument", typeof(ProbeOnce[])) with { RequiredParameterCount = 1, Execute = null };
    ActionRosterEntry agentOnly = Probe("Probe.AgentOnly", typeof(ProbeTwice[]));
    ActionCatalog catalog = new([new ActionCatalogSource([Metadata("Probe.AgentOnly", typeof(ProbeTwice[]), null, ActionVisibility.Agent)])]);

    string[] targets =
    [
      .. (await CommandPaletteRoster.BuildAsync(
        spa.User, spa.ServiceProvider.GetRequiredService<IAuthorizationService>(), [],
        [optedOut, blankReason, needsArgument, agentOnly], catalog.Entries, [], "/"))
        .Select(static row => row.Target)
    ];

    targets.ShouldBe(["Probe.BlankReason"], "A blank opt-out reason does not opt out.");
  }

  public static async Task Enter_On_Sign_In_Navigates_To_Login_With_The_Current_Path_As_Return_Url()
  {
    using PaletteSpa spa = new(permissions: null);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    NavigationManager navigationManager = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    navigationManager.NavigateTo("/Counter");

    await scope.Send(new CommandPaletteState.OpenActionSet.Action());
    await scope.Send(new CommandPaletteState.FilterActionSet.Action("login"));
    CommandPaletteRow highlighted = scope.Store.GetState<CommandPaletteState>().Highlighted.ShouldNotBeNull();
    highlighted.Kind.ShouldBe(CommandPaletteRowKind.Page);

    await CommandPaletteRunner.RunAsync(
      highlighted,
      scope.Store,
      ActionRoster.All,
      CancellationToken.None);

    navigationManager.Uri.ShouldBe("http://localhost/Login?returnUrl=%2FCounter");
  }

  public static Task Rank_Prefix_Then_Word_Start_Then_Substring_Then_Description_Then_Subsequence()
  {
    CommandPaletteRow[] rows =
    [
      Row("Unsettled", "no match in description"),
      Row("Site settings", "word start"),
      Row("Settings", "name prefix"),
      Row("Profile", "open the settings of the profile"),
      Row("Setup tooling", "tools"),
      Row("Chat", "nothing"),
    ];

    CommandPaletteRanker.Rank(rows, "sett").Select(static row => row.Name)
      .ShouldBe(["Settings", "Site settings", "Unsettled", "Profile", "Setup tooling"]);

    // Subsequence only: equal tier, so shorter name, then name.
    CommandPaletteRanker.Rank(rows, "stg").Select(static row => row.Name)
      .ShouldBe(["Settings", "Setup tooling", "Site settings"]);

    CommandPaletteRanker.Rank(rows, "zzz").ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Break_Ties_By_Shorter_Name_Then_Name()
  {
    CommandPaletteRow[] rows = [Row("Counter demo", ""), Row("Counter", ""), Row("Count", "")];

    CommandPaletteRanker.Rank(rows, "coun").Select(static row => row.Name)
      .ShouldBe(["Count", "Counter", "Counter demo"]);
    return Task.CompletedTask;
  }

  public static async Task Filter_Highlight_Best_Match_And_Clear_Highlight_On_No_Match()
  {
    using PaletteSpa spa = new(Everything);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new CommandPaletteState.OpenActionSet.Action());

    await scope.Send(new CommandPaletteState.FilterActionSet.Action("sign out"));
    CommandPaletteState palette = scope.Store.GetState<CommandPaletteState>();
    palette.Query.ShouldBe("sign out");
    palette.Highlighted.ShouldNotBeNull().Target.ShouldBe("Profile.SignOut");

    await scope.Send(new CommandPaletteState.FilterActionSet.Action("qqqzzz"));
    palette = scope.Store.GetState<CommandPaletteState>();
    palette.Matches.ShouldBeEmpty();
    palette.HighlightedIndex.ShouldBe(-1);
    palette.Highlighted.ShouldBeNull("Enter must not run a near-miss when nothing matches.");
  }

  public static async Task Move_Highlight_With_Arrows_And_Wrap()
  {
    using PaletteSpa spa = new(Everything);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new CommandPaletteState.OpenActionSet.Action());
    int count = scope.Store.GetState<CommandPaletteState>().Matches.Count;
    count.ShouldBeGreaterThan(2);

    await scope.Send(new CommandPaletteState.MoveHighlightActionSet.Action(1));
    scope.Store.GetState<CommandPaletteState>().HighlightedIndex.ShouldBe(1);

    await scope.Send(new CommandPaletteState.MoveHighlightActionSet.Action(-1));
    await scope.Send(new CommandPaletteState.MoveHighlightActionSet.Action(-1));
    scope.Store.GetState<CommandPaletteState>().HighlightedIndex.ShouldBe(count - 1);

    await scope.Send(new CommandPaletteState.MoveHighlightActionSet.Action(1));
    scope.Store.GetState<CommandPaletteState>().HighlightedIndex.ShouldBe(0);
  }

  public static async Task Enter_Navigates_To_The_Highlighted_Page()
  {
    using PaletteSpa spa = new(Everything);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new CommandPaletteState.OpenActionSet.Action());
    await scope.Send(new CommandPaletteState.FilterActionSet.Action("counter"));

    CommandPaletteRow highlighted = scope.Store.GetState<CommandPaletteState>().Highlighted.ShouldNotBeNull();
    highlighted.Kind.ShouldBe(CommandPaletteRowKind.Page);
    highlighted.Target.ShouldBe("/Counter");

    await CommandPaletteRunner.RunAsync(
      highlighted,
      scope.Store,
      ActionRoster.All,
      CancellationToken.None);

    scope.ServiceProvider.GetRequiredService<NavigationManager>().Uri.ShouldBe("http://localhost/Counter");
  }

  public static async Task Enter_Executes_A_Parameterless_Command()
  {
    using PaletteSpa spa = new(Everything);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    int executions = 0;
    ActionRosterEntry probe = Probe("Probe.Run", typeof(ProbeOnce)) with
    {
      Execute = (_, _) =>
      {
        executions++;
        return Task.CompletedTask;
      },
    };

    IReadOnlyList<CommandPaletteRow> roster = await CommandPaletteRoster.BuildAsync(
      spa.User, spa.ServiceProvider.GetRequiredService<IAuthorizationService>(), [], [probe], [], [], "/");
    CommandPaletteRow row = CommandPaletteRanker.Rank(roster, "probe")[0];
    row.Kind.ShouldBe(CommandPaletteRowKind.Command);

    await CommandPaletteRunner.RunAsync(row, scope.Store, [probe], CancellationToken.None);

    executions.ShouldBe(1);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().Uri.ShouldBe("http://localhost/");
  }

  public static async Task Report_A_Vanished_Command_In_The_Shell_Region()
  {
    using PaletteSpa spa = new(Everything);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    CommandPaletteRow stale = new("Gone: Away", "", CommandPaletteRowKind.Command, "Gone.Away");

    await CommandPaletteRunner.RunAsync(
      stale, scope.Store, ActionRoster.All, CancellationToken.None);

    scope.Store.GetState<NotificationState>().Messages
      .ShouldContain(message => message.Title == "Gone: Away is no longer available.");
  }

  public static async Task Render_Overlay_Only_While_It_Is_The_Active_Modal()
  {
    using PaletteSpa spa = new(Everything);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new CommandPaletteState.OpenActionSet.Action());

    string closed = await RenderShellModalsAsync(scope.ServiceProvider, activeModalId: null);
    closed.ShouldNotContain("data-qa=\"CommandPalette\"");

    await scope.Send(new ApplicationState.SetActiveModalActionSet.Action(CommandPalette.ModalId));
    string open = await RenderShellModalsAsync(scope.ServiceProvider, CommandPalette.ModalId);
    open.ShouldContain("data-qa=\"CommandPalette\"");
    open.ShouldContain("data-qa=\"CommandPaletteInput\"");
    open.ShouldContain("role=\"combobox\"");
    open.ShouldContain(">Counter<");
    open.ShouldContain(">Profile: Sign out<");
    open.ShouldContain(">Agent surface: Open ask panel<");
    open.ShouldNotContain("CommandPaletteAsk");
    open.ShouldContain("aria-activedescendant=\"twe-palette-row-0\"");

    await scope.Send(new ApplicationState.CloseModalActionSet.Action());
    scope.Store.GetState<ApplicationState>().ActiveModalId.ShouldBeNull();
  }

  private static async Task<string> RenderShellModalsAsync(IServiceProvider services, string? activeModalId)
  {
    await using HtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
    return await renderer.Dispatcher.InvokeAsync(async () =>
    {
      ParameterView parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
      {
        [nameof(ModalController.ActiveModalId)] = activeModalId,
        [nameof(ModalController.ModalContainers)] = (RenderFragment)(builder =>
        {
          builder.OpenComponent<CommandPalette>(0);
          builder.CloseComponent();
        }),
      });
      return (await renderer.RenderComponentAsync<ModalController>(parameters)).ToHtmlString();
    });
  }

  private static ActionRosterEntry Probe(string name, Type actionType) =>
    new(name, typeof(CounterState), actionType, 0, null, static (_, _) => Task.CompletedTask);

  private static ActionCatalogEntry Metadata(string name, Type actionType, string? displayName, ActionVisibility visibility = ActionVisibility.Human) =>
    new
    (
      name,
      "Run the probe.",
      [],
      visibility,
      typeof(CounterState),
      actionType,
      [],
      "{}",
      static (_, _, _) => Task.CompletedTask,
      displayName
    );

  // Distinct probe Action types: roster rows join catalog metadata by Action type.
  private sealed class ProbeOnce;

  private sealed class ProbeTwice;

  private sealed class AskOverride : IActionDisplayOverride
  {
    public ActionDisplay? Find(ActionRosterEntry action) =>
      action.Name == "AgentSurface.OpenAskPanel" ? new ActionDisplay("Ask Akili", "Open the docked assistant panel.") : null;
  }

  private static CommandPaletteRow Row(string name, string description) =>
    new(name, description, CommandPaletteRowKind.Page, "/" + name);

  /// <summary>In-proc SPA container with a fixed principal; <c>null</c> permissions = anonymous.</summary>
  private sealed class PaletteSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public ClaimsPrincipal User { get; }

    public PaletteSpa(string[]? permissions)
    {
      User = permissions is null
        ? new ClaimsPrincipal(new ClaimsIdentity())
        : new ClaimsPrincipal(new ClaimsIdentity(
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
      services.AddTimeWarpStateBlazor();
      services.AddActionCatalog(typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly);
      services.AddTimeWarpStatePersistence();
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(User));
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
}
