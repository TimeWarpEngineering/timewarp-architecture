#region Purpose
// Command palette Enter, ranking, highlight, and shell-overlay tests.
#endregion

namespace CommandPalette_;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Features;

[TestTag("Unit")]
public partial class CommandPalette_Should_
{

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
}
