#region Purpose
// Proves the Settings and Passkeys prerender facts: Microsoft 365 offered flag, Link / Unlink / Revoke
// availability and hints follow the server flags (tasks 229, 246, 282, 253).
#endregion

// Real HTTP through the ASP.NET pipeline (not WebTestServerApplication.Send / mediator) — only that
// path exercises identity-session cookie OnRedirectToLogin / OnRedirectToAccessDenied. Isolated
// HttpClient per test with AllowAutoRedirect=false so Location and status are observable. Passkey
// mint via CredentialCeremonyHelpers for signed-in cases (Member default; Admin/Roles needs
// Administrator so Member proves forbid ≠ Login). Cases mirror the dual-mode matrix in
// IdentitySessionCookieChallenge Design: HTML Accept + Accept-less fallback redirect; api/Roles
// anonymous 401; Member session cookie authenticated; Member /Admin/Roles 403.
// Task 183 resolved the old residual (authenticated HTML SSR stack overflow): hosted prerender
// now prefers HttpContext.User (HostedIdentitySessionAuthenticationStateProvider) and
// RedirectToLogin no longer navigates during static SSR. The Ok_Page_* cases below are the
// regression tripwire — pre-183 they killed the process (exit 134) instead of returning 200.
// They also assert the body is the real page, not RedirectToLogin's static sign-in fallback,
// so a silently-anonymous prerender state fails the test rather than passing on status alone.
// Task 229: Settings prerender fetches credentials so Link is hidden when an EntraAccount is
// linked, Unlink is disabled (with hint) when it is the last active credential, and Unlink is
// enabled when a passkey remains.
// Task 246: the passkey row action is Revoke and it is disabled (with the RevokePasskeyHint text)
// when the row is the last active credential of ANY kind — an agent key on the same principal
// re-enables it even though the page lists only passkeys (same count as RevokeCredential.Handler).
// Pinned on Settings (Member) and on the Developer-gated /Passkeys demo page.
// Task 282: Link, Unlink and Revoke follow the server's GetCredentials availability flags (no client
// predicate), so these prerender facts prove the real handler's flags reach the rendered buttons end to end.
// Task 253: Settings prerender shows "Signed in · TimeWarp account · <fingerprint>" — the hosted
// auth-state provider derives the fingerprint claim from the cookie principal (no HTTP loopback).

namespace ProtectedPageDeepLink_;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using SignInOptions = TimeWarp.Architecture.Features.Identity.Application.SignInOptions;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;
using static ProtectedPageDeepLink_.ProtectedPageDeepLinkFixture;

public class Settings_Returns_
{
  public static Task SetupOnce() => ProtectedPageDeepLinkFixture.AcquireAsync();

  public static Task CleanUpOnce() => ProtectedPageDeepLinkFixture.ReleaseAsync();

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Settings_Returns_>();

  public static async Task Settings_Microsoft365_Section_Should_Follow_Server_Offered_Flag()
  {
    (PrincipalId principalId, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    await SetRolesAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);

    HttpResponseMessage hidden = await GetPageHtml("/Settings", sessionCookie);
    string hiddenHtml = await hidden.Content.ReadAsStringAsync();
    hiddenHtml.ShouldNotContain("data-qa=\"Microsoft365Settings\"");
    hiddenHtml.ShouldNotContain("data-qa=\"LinkMicrosoft365\"");

    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    SignInOptions signIn =
      scope.ServiceProvider.GetRequiredService<IOptions<SignInOptions>>().Value;
    ISiteSettingsStore store = scope.ServiceProvider.GetRequiredService<ISiteSettingsStore>();
    SiteSettings? settings = await store.GetAsync();
    settings.ShouldNotBeNull();
    List<SignInMethod> previousMethods = [.. signIn.Methods];
    bool previousSignIn = settings!.EntraSignInEnabled;
    bool previousBootstrap = settings.EntraAllowBootstrap;
    PasskeyPromptMode previousMode = settings.PasskeyPromptMode;
    signIn.Methods = [SignInMethod.Passkey, SignInMethod.Microsoft365];
    settings.ReplacePolicy(true, previousBootstrap, previousMode);
    await store.UpdateAsync(settings);
    try
    {
      HttpResponseMessage shown = await GetPageHtml("/Settings", sessionCookie);
      string shownHtml = await shown.Content.ReadAsStringAsync();
      shownHtml.ShouldContain("data-qa=\"Microsoft365Settings\"");
      shownHtml.ShouldContain("data-qa=\"LinkMicrosoft365\"");
    }
    finally
    {
      signIn.Methods = previousMethods;
      SiteSettings? restore = await store.GetAsync();
      restore.ShouldNotBeNull();
      restore!.ReplacePolicy(previousSignIn, previousBootstrap, previousMode);
      await store.UpdateAsync(restore);
    }
  }

  public static async Task Settings_Linked_Entra_With_Passkey_Should_Hide_Link_And_Enable_Unlink()
  {
    (PrincipalId principalId, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    await SetRolesAsync(principalId, [RoleIds.Member]);
    const string accountHint = "Steven.Cramer@TimeWarp.Enterprises";
    await using IAsyncDisposable offered = await EnableMicrosoft365OfferedAsync();
    await AddEntraAccountAsync(principalId, accountHint);

    HttpResponseMessage response = await GetPageHtml("/Settings", sessionCookie);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string html = await response.Content.ReadAsStringAsync();
    html.ShouldContain("data-qa=\"Microsoft365Settings\"");
    html.ShouldContain("data-qa=\"Microsoft365AccountLabel\"");
    html.ShouldContain(accountHint);
    html.ShouldContain(">Microsoft 365<");
    html.ShouldNotContain("data-qa=\"LinkMicrosoft365\"");
    html.ShouldContain("data-qa=\"Unlink\"");
    FindTagContaining(html, "data-qa=\"Unlink\"").ShouldNotContain("disabled");
    html.ShouldNotContain("data-qa=\"UnlinkMicrosoft365Hint\"");
  }

  public static async Task Settings_Entra_Only_Should_Disable_Unlink_With_Hint()
  {
    (PrincipalId principalId, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    await SetRolesAsync(principalId, [RoleIds.Member]);
    const string accountHint = "Steven.Cramer@TimeWarp.Enterprises";
    await using IAsyncDisposable offered = await EnableMicrosoft365OfferedAsync();
    await AddEntraAccountAsync(principalId, accountHint);
    await RevokePasskeysAsync(principalId);

    HttpResponseMessage response = await GetPageHtml("/Settings", sessionCookie);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string html = await response.Content.ReadAsStringAsync();
    html.ShouldContain("data-qa=\"Microsoft365Settings\"");
    html.ShouldContain(accountHint);
    html.ShouldNotContain("data-qa=\"LinkMicrosoft365\"");
    html.ShouldContain("data-qa=\"Unlink\"");
    FindTagContaining(html, "data-qa=\"Unlink\"").ShouldContain("disabled");
    html.ShouldContain("data-qa=\"UnlinkMicrosoft365Hint\"");
    html.ShouldContain("Add a passkey first");
  }

  public static async Task Settings_Single_Passkey_Should_Disable_Revoke_With_Hint()
  {
    (PrincipalId principalId, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    await SetRolesAsync(principalId, [RoleIds.Member]);

    HttpResponseMessage response = await GetPageHtml("/Settings", sessionCookie);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string html = await response.Content.ReadAsStringAsync();
    html.ShouldContain("data-qa=\"RevokePasskey\"");
    html.ShouldNotContain("data-qa=\"DeletePasskey\"");
    FindTagContaining(html, "data-qa=\"RevokePasskey\"").ShouldContain("disabled");
    html.ShouldContain("data-qa=\"RevokePasskeyHint\"");
    html.ShouldContain(LastCredentialHint);
  }

  public static async Task Settings_Passkey_Plus_Agent_Key_Should_Enable_Revoke()
  {
    (PrincipalId principalId, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    await SetRolesAsync(principalId, [RoleIds.Member]);
    await AddAgentKeyAsync(principalId, "ganda");

    HttpResponseMessage response = await GetPageHtml("/Settings", sessionCookie);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string html = await response.Content.ReadAsStringAsync();
    html.ShouldContain("data-qa=\"RevokePasskey\"");
    FindTagContaining(html, "data-qa=\"RevokePasskey\"").ShouldNotContain("disabled");
    html.ShouldNotContain("data-qa=\"RevokePasskeyHint\"");
    html.ShouldNotContain(LastCredentialHint);
  }

  public static async Task Passkeys_Page_Single_Passkey_Should_Disable_Revoke_With_Hint()
  {
    (PrincipalId principalId, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    await SetRolesAsync(principalId, [RoleIds.Developer]);

    HttpResponseMessage response = await GetPageHtml("/Passkeys", sessionCookie);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string html = await response.Content.ReadAsStringAsync();
    html.ShouldContain("data-qa=\"RevokePasskey\"");
    FindTagContaining(html, "data-qa=\"RevokePasskey\"").ShouldContain("disabled");
    html.ShouldContain("data-qa=\"RevokePasskeyHint\"");
    html.ShouldContain(LastCredentialHint);
  }

  public static async Task Passkeys_Page_Passkey_Plus_Agent_Key_Should_Enable_Revoke()
  {
    (PrincipalId principalId, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    await SetRolesAsync(principalId, [RoleIds.Developer]);
    await AddAgentKeyAsync(principalId, "ganda");

    HttpResponseMessage response = await GetPageHtml("/Passkeys", sessionCookie);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string html = await response.Content.ReadAsStringAsync();
    FindTagContaining(html, "data-qa=\"RevokePasskey\"").ShouldNotContain("disabled");
    html.ShouldNotContain("data-qa=\"RevokePasskeyHint\"");
  }
}
