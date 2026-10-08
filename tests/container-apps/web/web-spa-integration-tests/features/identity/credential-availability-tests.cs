#region Purpose
// Server-owned availability on the Credentials pages (task 282): CredentialsState holds the server's
// typed flags (CanRevoke / CanRename per row, CanLinkMicrosoft365), Revoke and Rename dispatch the real
// actions with the row's id, and Ctrl-K on Settings and Passkeys lists only the normal roster.
#endregion

#region Design
// C-create in-proc SPA ServiceProvider per test (AGENTS.md default): real TimeWarp.State pipeline, real
// IActionCatalog and CommandPaletteState.Open, a TestNavigationManager for the current path, and the
// shared ScriptedCredentialsApiService standing in for the BFF. The script sets the flags with the
// server's real CredentialRules and applies the real effects (revoke/rename), so "the follow-up fetch
// changes the flags" is observed end to end on the client; the flags against the real handler are
// pinned in web-server-integration-tests (credential-availability-tests), and the rendered buttons in
// the prerender facts (protected-page-deep-link-tests) and CredentialList's render tests.
// CanRevokeOverride makes the server's answer contradict the credential count, so "follows the
// server" is distinguishable from "counts on the client". The page buttons dispatch the same action
// then FetchCredentials this file sends (there is no bUnit for the click itself).
#endregion

namespace CredentialAvailability_;

using FakeItEasy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Features.Identity;
using TimeWarp.Identity;
using CredentialSummary = TimeWarp.Architecture.Features.Identity.GetCredentials.CredentialSummary;

[TestTag("Integration")]
public class CredentialAvailability_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CredentialAvailability_Should_>();

  // --- flags come from the server -----------------------------------------------------------

  public static async Task Hold_The_Servers_Flags_Counting_Every_Active_Kind()
  {
    using AvailabilitySpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    CredentialSummary passkey = spa.AddPasskey("Work laptop", "aaaa1111");
    spa.Add(ScriptedCredentialsApiService.Active(CredentialType.AgentKey, "ganda"));

    await OpenAsync(scope, SettingsPath);

    CredentialsState state = scope.Store.GetState<CredentialsState>();
    Row(state, passkey).CanRevoke.ShouldBeTrue("the server counts the agent key too");
    Row(state, passkey).CanRename.ShouldBeTrue();
    state.CanLinkMicrosoft365.ShouldBeFalse();
  }

  public static async Task Follow_The_Server_When_Its_Flags_Contradict_The_Count()
  {
    using AvailabilitySpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    CredentialSummary first = spa.AddPasskey("Work laptop", "aaaa1111");
    CredentialSummary second = spa.AddPasskey("Phone", "bbbb2222");

    // Two active passkeys — a client count would allow Revoke — but the server says no.
    spa.Api.CanRevokeOverride = false;
    await OpenAsync(scope, SettingsPath);
    CredentialsState state = scope.Store.GetState<CredentialsState>();
    state.ActivePasskeys.ShouldAllBe(static credential => !credential.CanRevoke);
    Row(state, first).CanRename.ShouldBeTrue("the rest of the server's answer is untouched");

    // The reverse: one active passkey — a client count would forbid Revoke — but the server allows it.
    spa.Api.CanRevokeOverride = true;
    spa.Api.Credentials.Remove(second);
    await scope.Send(new CredentialsState.FetchCredentialsActionSet.Action());
    Row(scope.Store.GetState<CredentialsState>(), first).CanRevoke.ShouldBeTrue();
  }

  public static async Task Hold_CanLinkMicrosoft365_Only_While_The_Server_Allows_It()
  {
    using AvailabilitySpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.AddPasskey("Work laptop", "aaaa1111");
    spa.Api.Microsoft365Offered = true;

    await OpenAsync(scope, SettingsPath);
    scope.Store.GetState<CredentialsState>().CanLinkMicrosoft365.ShouldBeTrue();

    spa.Add(ScriptedCredentialsApiService.Active(CredentialType.EntraAccount, "Microsoft 365"));
    await scope.Send(new CredentialsState.FetchCredentialsActionSet.Action());
    scope.Store.GetState<CredentialsState>().CanLinkMicrosoft365.ShouldBeFalse("an account is linked now");
  }

  // --- buttons dispatch the real actions ----------------------------------------------------

  public static async Task Revoke_Dispatches_The_Real_Action_And_Refreshes_The_Flags()
  {
    using AvailabilitySpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    CredentialSummary first = spa.AddPasskey("Work laptop", "aaaa1111");
    CredentialSummary second = spa.AddPasskey(nickname: null, "bbbb2222");
    await OpenAsync(scope, SettingsPath);

    // What the page's Confirm revoke does: RevokeCredential(id), then FetchCredentials.
    await scope.Send(new CredentialsState.RevokeCredentialActionSet.Action(first.Id.Value));
    await scope.Send(new CredentialsState.FetchCredentialsActionSet.Action());

    spa.Api.Requests.OfType<RevokeCredential.Command>().Single().CredentialId.ShouldBe(first.Id.Value);
    Messages(scope).ShouldContain("Credential revoked.");
    CredentialsState state = scope.Store.GetState<CredentialsState>();
    CredentialSummary remaining = state.ActivePasskeys.ShouldHaveSingleItem();
    remaining.Id.ShouldBe(second.Id);
    remaining.CanRevoke.ShouldBeFalse("the last active credential");
  }

  public static async Task Rename_Dispatches_The_Real_Action_With_The_Typed_Nickname()
  {
    using AvailabilitySpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    CredentialSummary only = spa.AddPasskey("Work laptop", "aaaa1111");
    await OpenAsync(scope, PasskeysPath);

    // What the row's Save does: RenameCredential(id, nickname), then FetchCredentials.
    await scope.Send(new CredentialsState.RenameCredentialActionSet.Action(only.Id.Value, "Desk key"));
    await scope.Send(new CredentialsState.FetchCredentialsActionSet.Action());

    RenameCredential.Command command = spa.Api.Requests.OfType<RenameCredential.Command>().Single();
    command.CredentialId.ShouldBe(only.Id.Value);
    command.Nickname.ShouldBe("Desk key");
    Messages(scope).ShouldContain("Nickname saved.");
    Row(scope.Store.GetState<CredentialsState>(), only).Nickname.ShouldBe("Desk key");
  }

  // --- Ctrl-K: the normal roster only -------------------------------------------------------

  public static async Task Show_Only_The_Normal_Roster_On_Settings_And_Passkeys()
  {
    using AvailabilitySpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.AddPasskey("Work laptop", "aaaa1111");
    spa.AddPasskey(nickname: null, "bbbb2222");
    spa.Api.Microsoft365Offered = true;

    IReadOnlyList<CommandPaletteRow> elsewhere = await PaletteRosterAsync(scope, "/Counter");

    foreach (string path in new[] { SettingsPath, PasskeysPath })
    {
      await OpenAsync(scope, path);
      IReadOnlyList<CommandPaletteRow> roster = await PaletteRosterAsync(scope, path);

      roster.ShouldBe(elsewhere, $"{path} adds no rows of its own");
      roster.ShouldAllBe(static row => row.Kind == CommandPaletteRowKind.Page || row.Kind == CommandPaletteRowKind.Command);
      roster.ShouldNotContain(static row => row.Target == "Credentials.RevokeCredential" || row.Target == "Credentials.RenameCredential");
      roster.Count(static row => row.Target == "Credentials.LinkMicrosoft365").ShouldBe(1, "a general command, listed once");
    }
  }

  // --- helpers ------------------------------------------------------------------------------

  private const string SettingsPath = "/Settings";
  private const string PasskeysPath = "/Passkeys";

  private static async Task OpenAsync(SpaTestScope scope, string path)
  {
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo(path);
    await scope.Send(new CredentialsState.FetchCredentialsActionSet.Action());
  }

  private static async Task<IReadOnlyList<CommandPaletteRow>> PaletteRosterAsync(SpaTestScope scope, string path)
  {
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo(path);
    await scope.Send(new CommandPaletteState.OpenActionSet.Action());
    return scope.Store.GetState<CommandPaletteState>().Roster;
  }

  private static CredentialSummary Row(CredentialsState state, CredentialSummary credential) =>
    state.Credentials.ShouldNotBeNull().Single(row => row.Id == credential.Id);

  private static string[] Messages(SpaTestScope scope) =>
    [.. scope.Store.GetState<NotificationState>().Messages.Select(static message => message.Title)];

  /// <summary>In-proc SPA with a signed-in principal, the real catalog and palette, and the scripted credentials BFF.</summary>
  private sealed class AvailabilitySpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public ScriptedCredentialsApiService Api { get; } = new();

    public AvailabilitySpa()
    {
      ClaimsPrincipal user = new(new ClaimsIdentity(
        [
          new Claim("sub", Guid.NewGuid().ToString()),
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

    public CredentialSummary Add(CredentialSummary credential)
    {
      Api.Credentials.Add(credential);
      return credential;
    }

    public CredentialSummary AddPasskey(string? nickname, string fingerprint) =>
      Add(new CredentialSummary(CredentialId.New(), CredentialType.Passkey, "1Password", nickname, DateTimeOffset.UtcNow.AddDays(-1), revokedAt: null, isActive: true, RegisteredWith.Unknown, fingerprint));

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
