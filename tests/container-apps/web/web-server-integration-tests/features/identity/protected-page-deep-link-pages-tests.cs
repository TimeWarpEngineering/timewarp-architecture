#region Purpose
// Proves signed-out HTML deep links to protected pages 302 to /Login?returnUrl=… while /api stays 401
// and authenticated forbid stays 403, plus the authenticated admin pages (task 154, 183).
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
// Settings and Passkeys prerender facts (tasks 229, 246, 282, 253) are in
// protected-page-deep-link-settings-tests.

namespace ProtectedPageDeepLink_;

using System.Net;
using System.Net.Http.Headers;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;
using static ProtectedPageDeepLink_.ProtectedPageDeepLinkFixture;

public class Pages_Returns_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  public static async Task SetupOnce() => Graph = await ProtectedPageDeepLinkFixture.CreateHostAsync();

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Pages_Returns_>();

  public static async Task Redirect_To_Login_Given_Signed_Out_Settings_With_Html_Accept()
  {
    using HttpClient client = CreateNoRedirectClient(Web);
    using HttpRequestMessage request = new(HttpMethod.Get, "/Settings");
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

    HttpResponseMessage response = await client.SendAsync(request);

    AssertRedirectToLogin(response, expectedReturnUrl: "/Settings");
  }

  public static async Task Redirect_To_Login_Given_Signed_Out_Settings_Without_Accept()
  {
    using HttpClient client = CreateNoRedirectClient(Web);

    HttpResponseMessage response = await client.GetAsync("/Settings");

    AssertRedirectToLogin(response, expectedReturnUrl: "/Settings");
  }

  public static async Task Unauthorized_Given_Anonymous_Api_Roles()
  {
    using HttpClient client = CreateNoRedirectClient(Web);

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

    using HttpClient client = CreateNoRedirectClient(Web);
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

    using HttpClient client = CreateNoRedirectClient(Web);
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
    await SetRolesAsync(Web, principalId, [RoleIds.Member]);

    HttpResponseMessage response = await GetPageHtml(Web, "/Settings", sessionCookie);

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

  public static async Task Forbidden_Not_Login_Given_Passkey_Member_Admin_Authentication_Html()
  {
    (PrincipalId principalId, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    await SetRolesAsync(Web, principalId, [RoleIds.Member]);

    using HttpClient client = CreateNoRedirectClient(Web);
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
    await SetRolesAsync(Web, principalId, [RoleIds.Member, RoleIds.Administrator]);

    HttpResponseMessage response = await GetPageHtml(Web, "/Admin/Authentication", sessionCookie);

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
    await SetRolesAsync(Web, principalId, [RoleIds.Member, RoleIds.Administrator]);

    HttpResponseMessage response = await GetPageHtml(Web, "/Admin/Roles", sessionCookie);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string html = await response.Content.ReadAsStringAsync();
    html.ShouldContain("data-qa=\"NewRole\"");
    html.ShouldContain("aria-label=\"Breadcrumb\"");
    html.ShouldContain("data-qa=\"BreadcrumbCurrent\"");
    html.ShouldNotContain("data-qa=\"BackToRoles\"");
    html.ShouldNotContain("Sign in to continue",
      customMessage: "Prerender rendered RedirectToLogin's fallback — auth state was anonymous despite a valid cookie.");
  }
}
