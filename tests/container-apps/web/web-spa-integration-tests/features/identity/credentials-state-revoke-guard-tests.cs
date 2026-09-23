#region Purpose
// Task 246: the SPA disables Revoke on the last active credential using the SAME count as
// RevokeCredential.Handler — every active CredentialType, not just the passkeys a page shows.
#endregion

#region Design
// C-create CredentialsSpaTestApplication (scripted IWebServerApiService + signed-in
// AuthenticationStateProvider): the facts drive real FetchCredentials / RevokeCredential
// ActionSets through the ClientPipeline and read CredentialsState after each Send (re-fetch the
// state — clone-on-dispatch replaces the instance). Disabled/enabled is asserted through the exact
// expression both pages bind to RevokeDisabled: CanUnlink(ActiveCredentialCount). The rendered
// button/hint markup is pinned by the prerender HTML facts in web-server-integration-tests
// (protected-page-deep-link-tests: RevokePasskey + RevokePasskeyHint).
#endregion

namespace CredentialsStateRevokeGuard_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Features.Identity;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Identity.CredentialsState;

[TestTag("Integration")]
public class Revoke_Should_
{
  private static CredentialsSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Revoke_Should_>();

  public static Task SetupOnce()
  {
    Spa = new CredentialsSpaTestApplication();
    return Task.CompletedTask;
  }

  public static Task Setup()
  {
    Spa!.Scripted.Reset();
    return Task.CompletedTask;
  }

  public static Task CleanUpOnce()
  {
    Spa?.Dispose();
    Spa = null;
    return Task.CompletedTask;
  }

  private static bool RevokeDisabled(SpaTestScope scope)
  {
    CredentialsState state = scope.Store.GetState<CredentialsState>();
    return !CanUnlink(state.ActiveCredentialCount);
  }

  /// <summary>Fetch must have reached the scripted BFF and produced a snapshot; notification bars explain a miss.</summary>
  private static CredentialsState LoadedState(SpaTestScope scope)
  {
    TimeWarp.Architecture.Features.NotificationState notification =
      scope.Store.GetState<TimeWarp.Architecture.Features.NotificationState>();
    string bars = string.Join(" | ", notification.Messages.Select(message => $"{message.Intent}:{message.Title}"));
    Spa!.Scripted.Requests.OfType<GetCredentials.Query>().ShouldNotBeEmpty($"FetchCredentials never called the BFF. Notifications: {bars}");
    CredentialsState state = scope.Store.GetState<CredentialsState>();
    state.Credentials.ShouldNotBeNull($"No credentials snapshot. Notifications: {bars}");
    return state;
  }

  public static async Task Be_Disabled_Given_One_Active_Passkey()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    Spa!.Scripted.Credentials.Add(ScriptedCredentialsApiService.Active(CredentialType.Passkey, "Proton Pass"));

    await scope.Send(new FetchCredentialsActionSet.Action());

    CredentialsState state = LoadedState(scope);
    state.ActivePasskeys.Count.ShouldBe(1);
    state.ActiveCredentialCount.ShouldBe(1);
    RevokeDisabled(scope).ShouldBeTrue();
  }

  public static async Task Not_Count_Revoked_Rows_Given_IncludeRevoked_Snapshot()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    Spa!.Scripted.Credentials.Add(ScriptedCredentialsApiService.Active(CredentialType.Passkey, "Proton Pass"));
    Spa.Scripted.Credentials.Add(ScriptedCredentialsApiService.Revoked(CredentialType.Passkey, "old phone"));
    Spa.Scripted.Credentials.Add(ScriptedCredentialsApiService.Revoked(CredentialType.AgentKey, "old agent"));

    await scope.Send(new FetchCredentialsActionSet.Action(includeRevoked: true));

    CredentialsState state = LoadedState(scope);
    state.Credentials!.Count.ShouldBe(3);
    state.ActiveCredentialCount.ShouldBe(1);
    RevokeDisabled(scope).ShouldBeTrue();
  }

  public static async Task Be_Enabled_Given_One_Passkey_And_One_Agent_Key()
  {
    // The page lists ONE passkey, but the server counts every active kind — so must the SPA.
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    Spa!.Scripted.Credentials.Add(ScriptedCredentialsApiService.Active(CredentialType.Passkey, "Proton Pass"));
    Spa.Scripted.Credentials.Add(ScriptedCredentialsApiService.Active(CredentialType.AgentKey, "ganda"));

    await scope.Send(new FetchCredentialsActionSet.Action());

    CredentialsState state = LoadedState(scope);
    state.ActivePasskeys.Count.ShouldBe(1);
    state.ActiveCredentialCount.ShouldBe(2);
    RevokeDisabled(scope).ShouldBeFalse();
  }

  public static async Task Flip_To_Disabled_After_Revoke_Leaves_One_Without_Reload()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    CredentialSummaryRows rows = new
    (
      ScriptedCredentialsApiService.Active(CredentialType.Passkey, "laptop"),
      ScriptedCredentialsApiService.Active(CredentialType.Passkey, "phone")
    );
    Spa!.Scripted.Credentials.AddRange([rows.Laptop, rows.Phone]);

    await scope.Send(new FetchCredentialsActionSet.Action());
    LoadedState(scope);
    RevokeDisabled(scope).ShouldBeFalse();

    // Same sequence PasskeysPage/Settings ConfirmRevokeAsync runs: Revoke then Fetch.
    await scope.Send(new RevokeCredentialActionSet.Action(rows.Phone.Id.Value));
    await scope.Send(new FetchCredentialsActionSet.Action());

    CredentialsState state = LoadedState(scope);
    TimeWarp.Architecture.Features.NotificationState notification =
      scope.Store.GetState<TimeWarp.Architecture.Features.NotificationState>();
    notification.Messages.ShouldContain(message =>
      message.Intent == MessageBarIntent.Success && message.Title == "Credential revoked.");
    state.ActivePasskeys.Count.ShouldBe(1);
    state.ActivePasskeys[0].Id.ShouldBe(rows.Laptop.Id);
    state.ActiveCredentialCount.ShouldBe(1);
    RevokeDisabled(scope).ShouldBeTrue();
    Spa.Scripted.Requests.OfType<RevokeCredential.Command>().Count().ShouldBe(1);
  }

  private sealed record CredentialSummaryRows
  (
    GetCredentials.CredentialSummary Laptop,
    GetCredentials.CredentialSummary Phone
  );
}
