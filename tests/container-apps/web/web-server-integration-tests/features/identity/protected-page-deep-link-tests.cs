#region Purpose
// Proves signed-out HTML deep links to protected pages 302 to /Login?returnUrl=… while /api stays 401
// and authenticated forbid stays 403 (task 154).
#endregion

#region Design
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
// Task 253: Settings prerender shows "Signed in · TimeWarp account · <fingerprint>" — the hosted
// auth-state provider derives the fingerprint claim from the cookie principal (no HTTP loopback).
#endregion

namespace ProtectedPageDeepLink_;

using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;

public class Returns_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Returns_>();

  public static async Task SetupOnce()
  {
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
#else
    Graph = await HostGraphFactory.CreateWebAsync();
#endif
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static async Task Redirect_To_Login_Given_Signed_Out_Settings_With_Html_Accept()
  {
    using HttpClient client = CreateNoRedirectClient();
    using HttpRequestMessage request = new(HttpMethod.Get, "/Settings");
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

    HttpResponseMessage response = await client.SendAsync(request);

    AssertRedirectToLogin(response, expectedReturnUrl: "/Settings");
  }

  public static async Task Redirect_To_Login_Given_Signed_Out_Settings_Without_Accept()
  {
    using HttpClient client = CreateNoRedirectClient();

    HttpResponseMessage response = await client.GetAsync("/Settings");

    AssertRedirectToLogin(response, expectedReturnUrl: "/Settings");
  }

  public static async Task Unauthorized_Given_Anonymous_Api_Roles()
  {
    using HttpClient client = CreateNoRedirectClient();

    HttpResponseMessage response = await client.GetAsync("api/Roles");

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    response.Headers.Location.ShouldBeNull();
  }

  public static async Task Ok_Authenticated_Session_Given_Passkey_Member_Cookie()
  {
    // Positive signed-in proof without Blazor page SSR (see Design residual on /Settings HTML).
    // Combined with Forbidden_Not_Login… this proves: cookie works, page forbid stays 403/no Login.
    (_, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);

    using HttpClient client = CreateNoRedirectClient();
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    HttpResponseMessage response = await client.GetAsync(GetCurrentSession.Query.RouteTemplate);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    response.Headers.Location.ShouldBeNull();
    string json = await response.Content.ReadAsStringAsync();
    json.Contains("\"isAuthenticated\":true", StringComparison.OrdinalIgnoreCase)
      .ShouldBeTrue($"Expected authenticated session payload, got: {json}");
  }

  public static async Task Forbidden_Not_Login_Given_Passkey_Member_Admin_Roles_Html()
  {
    (_, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);

    using HttpClient client = CreateNoRedirectClient();
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    using HttpRequestMessage request = new(HttpMethod.Get, "/Admin/Roles");
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

    HttpResponseMessage response = await client.SendAsync(request);

    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    response.Headers.Location.ShouldBeNull(
      "Authenticated forbid must stay 403 — never redirect to Login (task 154 / task 153 loop guard).");
  }

  public static async Task Ok_Page_Given_Passkey_Member_Settings_Html()
  {
    // Task 183 regression tripwire: pre-fix this stack-overflowed the process (exit 134) —
    // hosted prerender saw an anonymous principal and RedirectToLogin navigated during static SSR.
    (PrincipalId principalId, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    await SetRolesAsync(principalId, [RoleIds.Member]);

    HttpResponseMessage response = await GetPageHtml("/Settings", sessionCookie);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string html = await response.Content.ReadAsStringAsync();
    html.ShouldContain("Passkeys");
    html.ShouldContain("data-qa=\"CreatePasskey\"");
    html.ShouldContain("data-qa=\"AddExistingPasskey\"");
    // Task 253: the signed-in account line carries the same fingerprint as the passkey user name.
    html.ShouldContain("data-qa=\"SignedInAccount\"");
    html.ShouldContain(PrincipalFingerprint.Compute(principalId));
    html.ShouldNotContain("Sign in to continue",
      customMessage: "Prerender rendered RedirectToLogin's fallback — auth state was anonymous despite a valid cookie.");
    html.ShouldNotContain("data-qa=\"AuthenticationSettings\"");
    html.ShouldNotContain("data-qa=\"AuthenticationSave\"");
    html.ShouldNotContain("data-qa=\"Microsoft365Settings\"");
    html.ShouldNotContain("data-qa=\"LinkMicrosoft365\"");
  }

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
    EntraAuthenticationOptions options =
      scope.ServiceProvider.GetRequiredService<IOptions<EntraAuthenticationOptions>>().Value;
    ISiteSettingsStore store = scope.ServiceProvider.GetRequiredService<ISiteSettingsStore>();
    SiteSettings? settings = await store.GetAsync();
    settings.ShouldNotBeNull();
    bool previousEnabled = options.Enabled;
    bool previousSignIn = settings!.EntraSignInEnabled;
    bool previousBootstrap = settings.EntraAllowBootstrap;
    PasskeyPromptMode previousMode = settings.PasskeyPromptMode;
    options.Enabled = true;
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
      options.Enabled = previousEnabled;
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

  private const string LastCredentialHint = "Add another passkey or agent key before revoking this one.";

  public static async Task Forbidden_Not_Login_Given_Passkey_Member_Admin_Authentication_Html()
  {
    (PrincipalId principalId, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    await SetRolesAsync(principalId, [RoleIds.Member]);

    using HttpClient client = CreateNoRedirectClient();
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    using HttpRequestMessage request = new(HttpMethod.Get, "/Admin/Authentication");
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

    HttpResponseMessage response = await client.SendAsync(request);

    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    response.Headers.Location.ShouldBeNull(
      "Authenticated forbid must stay 403 — never redirect to Login (task 154 / task 153 loop guard).");
  }

  public static async Task Ok_Page_Given_Passkey_Administrator_Admin_Authentication_Html()
  {
    (PrincipalId principalId, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    await SetRolesAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);

    HttpResponseMessage response = await GetPageHtml("/Admin/Authentication", sessionCookie);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string html = await response.Content.ReadAsStringAsync();
    html.ShouldContain("data-qa=\"AuthenticationSettings\"");
    html.ShouldContain("data-qa=\"ConfigurationTenant\"");
    html.ShouldContain("Microsoft 365 sign-in policy");
    html.ShouldNotContain("data-qa=\"AddConfigurationTenant\"");
    html.ShouldNotContain("data-qa=\"SaveAuthenticationSettings\"");
    html.ShouldNotContain("Sign in to continue",
      customMessage: "Prerender rendered RedirectToLogin's fallback — auth state was anonymous despite a valid cookie.");
  }

  public static async Task Ok_Page_Given_Passkey_Administrator_Admin_Roles_Html()
  {
    // The original task 183 crash page: signed-in admin GET /Admin/Roles killed web-server.
    (PrincipalId principalId, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    await SetRolesAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);

    HttpResponseMessage response = await GetPageHtml("/Admin/Roles", sessionCookie);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string html = await response.Content.ReadAsStringAsync();
    html.ShouldContain("data-qa=\"NewRole\"");
    html.ShouldContain("aria-label=\"breadcrumb\"");
    html.ShouldNotContain("data-qa=\"BackToRoles\"");
    html.ShouldNotContain("Sign in to continue",
      customMessage: "Prerender rendered RedirectToLogin's fallback — auth state was anonymous despite a valid cookie.");
  }

  private static string FindTagContaining(string html, string marker)
  {
    int searchFrom = 0;
    while (searchFrom < html.Length)
    {
      int start = IndexOfOpeningTag(html, searchFrom);
      if (start < 0)
      {
        break;
      }

      int end = html.IndexOf('>', start);
      if (end < 0)
      {
        break;
      }

      string tag = html[start..(end + 1)];
      if (tag.Contains(marker, StringComparison.Ordinal))
      {
        return tag;
      }

      searchFrom = end + 1;
    }

    throw new ShouldAssertException($"Expected an opening button tag containing {marker}.");
  }

  private static int IndexOfOpeningTag(string html, int startIndex)
  {
    int fluentButton = html.IndexOf("<fluent-button", startIndex, StringComparison.OrdinalIgnoreCase);
    int button = html.IndexOf("<button", startIndex, StringComparison.OrdinalIgnoreCase);
    if (fluentButton < 0)
    {
      return button;
    }

    if (button < 0)
    {
      return fluentButton;
    }

    return Math.Min(fluentButton, button);
  }

  private static async Task<HttpResponseMessage> GetPageHtml(string path, string sessionCookie)
  {
    using HttpClient client = CreateNoRedirectClient();
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    using HttpRequestMessage request = new(HttpMethod.Get, path);
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
    return await client.SendAsync(request);
  }

  private static async Task SetRolesAsync(PrincipalId principalId, Guid[] roleIds)
  {
    // Scope required: under postgres IPrincipalRoleStore is scoped (EfPrincipalRoleStore).
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, roleIds);
  }

  // Task 250 shape: Label is the provider, the linked account is AccountHint (the row's context line).
  private static async Task AddEntraAccountAsync(PrincipalId principalId, string accountHint)
  {
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalStore principalStore = scope.ServiceProvider.GetRequiredService<IPrincipalStore>();
    Guid tenantId = Guid.NewGuid();
    await principalStore.AddCredentialAsync(
      Credential.Create(
        principalId,
        CredentialType.EntraAccount,
        EntraAccountHandle.Encode(tenantId, Guid.NewGuid()),
        EntraIssuerMaterial.FromTenantId(tenantId),
        label: "Microsoft 365",
        accountHint: accountHint));
  }

  private static async Task AddAgentKeyAsync(PrincipalId principalId, string label)
  {
    // Store-level insert (same shape as AddEntraAccountAsync): the count under test is "active
    // credentials of any kind", so the material only needs to be a distinct AgentKey row.
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalStore principalStore = scope.ServiceProvider.GetRequiredService<IPrincipalStore>();
    byte[] keyId = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
    byte[] publicMaterial = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
    await principalStore.AddCredentialAsync(
      Credential.Create(principalId, CredentialType.AgentKey, keyId, publicMaterial, label));
  }

  private static async Task RevokePasskeysAsync(PrincipalId principalId)
  {
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalStore principalStore = scope.ServiceProvider.GetRequiredService<IPrincipalStore>();
    IReadOnlyList<Credential> credentials = await principalStore.ListCredentialsAsync(principalId);
    foreach (Credential credential in credentials.Where(c => c.Type == CredentialType.Passkey && !c.IsRevoked))
    {
      credential.Revoke();
      await principalStore.UpdateCredentialAsync(credential);
    }
  }

  private static async Task<IAsyncDisposable> EnableMicrosoft365OfferedAsync()
  {
    AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    EntraAuthenticationOptions options =
      scope.ServiceProvider.GetRequiredService<IOptions<EntraAuthenticationOptions>>().Value;
    ISiteSettingsStore store = scope.ServiceProvider.GetRequiredService<ISiteSettingsStore>();
    SiteSettings? settings = await store.GetAsync();
    settings.ShouldNotBeNull();
    bool previousEnabled = options.Enabled;
    bool previousSignIn = settings!.EntraSignInEnabled;
    bool previousBootstrap = settings.EntraAllowBootstrap;
    PasskeyPromptMode previousMode = settings.PasskeyPromptMode;
    options.Enabled = true;
    settings.ReplacePolicy(true, previousBootstrap, previousMode);
    await store.UpdateAsync(settings);
    return new RestoreMicrosoft365Offered(scope, options, store, previousEnabled, previousSignIn, previousBootstrap, previousMode);
  }

  private sealed class RestoreMicrosoft365Offered : IAsyncDisposable
  {
    private readonly AsyncServiceScope Scope;
    private readonly EntraAuthenticationOptions Options;
    private readonly ISiteSettingsStore Store;
    private readonly bool PreviousEnabled;
    private readonly bool PreviousSignIn;
    private readonly bool PreviousBootstrap;
    private readonly PasskeyPromptMode PreviousMode;

    public RestoreMicrosoft365Offered(
      AsyncServiceScope scope,
      EntraAuthenticationOptions options,
      ISiteSettingsStore store,
      bool previousEnabled,
      bool previousSignIn,
      bool previousBootstrap,
      PasskeyPromptMode previousMode)
    {
      Scope = scope;
      Options = options;
      Store = store;
      PreviousEnabled = previousEnabled;
      PreviousSignIn = previousSignIn;
      PreviousBootstrap = previousBootstrap;
      PreviousMode = previousMode;
    }

    public async ValueTask DisposeAsync()
    {
      Options.Enabled = PreviousEnabled;
      SiteSettings? restore = await Store.GetAsync();
      restore.ShouldNotBeNull();
      restore!.ReplacePolicy(PreviousSignIn, PreviousBootstrap, PreviousMode);
      await Store.UpdateAsync(restore);
      await Scope.DisposeAsync();
    }
  }

  private static HttpClient CreateNoRedirectClient()
  {
    HttpClientHandler handler = new()
    {
      AllowAutoRedirect = false,
      CheckCertificateRevocationList = true,
    };
    return new HttpClient(handler) { BaseAddress = Web.HttpClient.BaseAddress };
  }

  private static void AssertRedirectToLogin(HttpResponseMessage response, string expectedReturnUrl)
  {
    response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    response.Headers.Location.ShouldNotBeNull();

    string location = response.Headers.Location!.IsAbsoluteUri
      ? response.Headers.Location.PathAndQuery
      : response.Headers.Location.OriginalString;

    // Exact shape from IdentitySessionCookieChallenge.BuildLoginRedirectTarget.
    string expected =
      IdentitySessionCookieChallenge.LoginPath
      + $"?{IdentitySessionCookieChallenge.ReturnUrlQueryParameter}"
      + $"={Uri.EscapeDataString(expectedReturnUrl)}";
    location.ShouldBe(expected);
  }
}
