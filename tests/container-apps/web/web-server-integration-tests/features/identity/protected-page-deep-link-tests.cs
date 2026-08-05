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
// Residual: full authenticated Blazor HTML SSR of /Settings stack-overflows in this in-proc host
// (IdentitySessionAuthenticationStateProvider + AuthenticationStateListener prerender path) —
// process dies before status is readable. Page 200 is left to live smoke; challenge/forbid and
// API 401 are covered here without that SSR path.
#endregion

namespace ProtectedPageDeepLink_;

using System.Net;
using System.Net.Http.Headers;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;

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
      $"{IdentitySessionCookieChallenge.LoginPath}"
      + $"?{IdentitySessionCookieChallenge.ReturnUrlQueryParameter}"
      + $"={Uri.EscapeDataString(expectedReturnUrl)}";
    location.ShouldBe(expected);
  }
}
