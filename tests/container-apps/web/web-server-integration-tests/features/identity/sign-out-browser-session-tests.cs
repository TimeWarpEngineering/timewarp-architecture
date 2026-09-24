#region Purpose
// Integration tests for the browser-facing sign-out (task 251): antiforgery-validated form POST
// clears the identity-session cookie in the BROWSER jar and 303s to /Login, in every render mode.
#endregion

#region Design
// Real HTTP through the ASP.NET pipeline with a per-test "browser": HttpClientHandler +
// CookieContainer (applies Set-Cookie exactly like a browser jar) and AllowAutoRedirect=false so
// the 303 and the protected-page 302 are observable. The flow mirrors sign-out.ts: GET the
// antiforgery token (sets the antiforgery cookie in the jar), then POST the form field.
// Server render mode: under InteractiveServer the old SPA path POSTed EndBrowserSession from the
// circuit through the named loopback HttpClient (IdentitySessionCookieForwardingHandler forwards
// the browser Cookie). Server_Mode_* reproduces that hop with the real forwarding handler over a
// circuit-like HttpContext and asserts the difference that is the bug: the loopback response
// carries the expired Set-Cookie, yet the browser jar still holds a valid session; the browser
// form POST then clears the jar and the session is anonymous. Cookie auth is stateless, so only
// the browser-visible cookie decides the outcome — that is what these facts pin.
#endregion

namespace SignOutBrowserSession_;

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;

public class SignOut_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;
  private static Uri BaseUri => Web.HttpClient.BaseAddress
    ?? throw new InvalidOperationException("Web HttpClient BaseAddress is null.");

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SignOut_>();

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

  public static async Task Clears_Browser_Cookie_And_Redirects_To_Login()
  {
    (_, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    CookieContainer jar = CreateJar(sessionCookie);
    using HttpClient browser = CreateBrowser(jar);

    (await IsAuthenticatedAsync(browser)).ShouldBeTrue("Precondition: minted session is signed in.");

    HttpResponseMessage response = await PostSignOutFormAsync(browser);

    response.StatusCode.ShouldBe(HttpStatusCode.SeeOther);
    response.Headers.Location.ShouldNotBeNull();
    response.Headers.Location!.OriginalString.ShouldBe(SignOutBrowserSession.RedirectPath);
    response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookies).ShouldBeTrue();
    setCookies!.ShouldContain(
      value => value.StartsWith(IdentitySessionDefaults.CookieName + "=", StringComparison.Ordinal)
        && value.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase),
      "Expected an expired identity-session Set-Cookie on the browser response.");

    HasSessionCookie(jar).ShouldBeFalse("The browser jar must no longer carry the identity-session cookie.");
    (await IsAuthenticatedAsync(browser)).ShouldBeFalse();
  }

  public static async Task Protected_Page_Redirects_To_Login_After_Sign_Out()
  {
    (_, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    CookieContainer jar = CreateJar(sessionCookie);
    using HttpClient browser = CreateBrowser(jar);

    HttpResponseMessage signOut = await PostSignOutFormAsync(browser);
    signOut.StatusCode.ShouldBe(HttpStatusCode.SeeOther);

    using HttpRequestMessage request = new(HttpMethod.Get, "/Settings");
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
    HttpResponseMessage page = await browser.SendAsync(request);

    page.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    page.Headers.Location.ShouldNotBeNull();
    page.Headers.Location!.OriginalString.ShouldStartWith(IdentitySessionCookieChallenge.LoginPath);
  }

  public static async Task BadRequest_And_Still_Signed_In_Given_Missing_Antiforgery_Token()
  {
    (_, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    CookieContainer jar = CreateJar(sessionCookie);
    using HttpClient browser = CreateBrowser(jar);

    // Cross-site forgery shape: a form POST with the ambient session cookie but no token.
    HttpResponseMessage response = await browser.PostAsync(
      SignOutBrowserSession.Path,
      new FormUrlEncodedContent([]));

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    response.Headers.Location.ShouldBeNull();
    HasSessionCookie(jar).ShouldBeTrue();
    (await IsAuthenticatedAsync(browser)).ShouldBeTrue("A rejected sign-out must not end the session.");
  }

  public static async Task BadRequest_Given_Token_Without_Its_Antiforgery_Cookie()
  {
    (_, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);

    // Token minted in one browser, replayed from another jar that never received the paired cookie.
    CookieContainer mintingJar = CreateJar(sessionCookie);
    using HttpClient mintingBrowser = CreateBrowser(mintingJar);
    SignOutBrowserSession.AntiforgeryToken token = await GetAntiforgeryTokenAsync(mintingBrowser);

    CookieContainer replayJar = CreateJar(sessionCookie);
    using HttpClient replayBrowser = CreateBrowser(replayJar);
    HttpResponseMessage response = await replayBrowser.PostAsync(
      SignOutBrowserSession.Path,
      new FormUrlEncodedContent([new(token.FormFieldName, token.RequestToken)]));

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    (await IsAuthenticatedAsync(replayBrowser)).ShouldBeTrue();
  }

  public static async Task Idempotent_Given_Anonymous_Browser()
  {
    CookieContainer jar = new();
    using HttpClient browser = CreateBrowser(jar);

    HttpResponseMessage response = await PostSignOutFormAsync(browser);

    response.StatusCode.ShouldBe(HttpStatusCode.SeeOther);
    response.Headers.Location!.OriginalString.ShouldBe(SignOutBrowserSession.RedirectPath);
  }

  public static async Task Server_Mode_Loopback_Leaves_Browser_Signed_In_Browser_Post_Clears_It()
  {
    (_, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    CookieContainer jar = CreateJar(sessionCookie);
    using HttpClient browser = CreateBrowser(jar);

    // The InteractiveServer circuit's HttpContext carries the browser's Cookie header.
    DefaultHttpContext circuitContext = new();
    circuitContext.Request.Headers.Cookie = jar.GetCookieHeader(BaseUri);
    circuitContext.Request.Host = new HostString(BaseUri.Authority);
    using HttpClient loopback = new(
      new IdentitySessionCookieForwardingHandler(new HttpContextAccessor { HttpContext = circuitContext })
      {
        InnerHandler = new HttpClientHandler { UseCookies = false, CheckCertificateRevocationList = true }
      })
    { BaseAddress = BaseUri };

    // Old SPA path under Server: EndBrowserSession over the server→server loopback.
    HttpResponseMessage loopbackResponse = await loopback.PostAsync(EndBrowserSession.Command.RouteTemplate, content: null);
    loopbackResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    loopbackResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? loopbackSetCookies).ShouldBeTrue();
    loopbackSetCookies!.ShouldContain(
      value => value.StartsWith(IdentitySessionDefaults.CookieName + "=", StringComparison.Ordinal),
      "The deletion exists — but only on the loopback HttpClient response.");

    HasSessionCookie(jar).ShouldBeTrue("The loopback deletion never reaches the browser jar (the task 251 bug).");
    (await IsAuthenticatedAsync(browser)).ShouldBeTrue("Browser is still signed in after the loopback sign-out.");

    // New path, identical in every render mode: the browser itself posts the sign-out form.
    HttpResponseMessage browserResponse = await PostSignOutFormAsync(browser);

    browserResponse.StatusCode.ShouldBe(HttpStatusCode.SeeOther);
    HasSessionCookie(jar).ShouldBeFalse("The browser form POST clears the browser-visible cookie.");
    (await IsAuthenticatedAsync(browser)).ShouldBeFalse();
  }

  private static async Task<HttpResponseMessage> PostSignOutFormAsync(HttpClient browser)
  {
    SignOutBrowserSession.AntiforgeryToken token = await GetAntiforgeryTokenAsync(browser);
    return await browser.PostAsync(
      SignOutBrowserSession.Path,
      new FormUrlEncodedContent([new(token.FormFieldName, token.RequestToken)]));
  }

  private static async Task<SignOutBrowserSession.AntiforgeryToken> GetAntiforgeryTokenAsync(HttpClient browser)
  {
    HttpResponseMessage response = await browser.GetAsync(SignOutBrowserSession.AntiforgeryTokenPath);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    SignOutBrowserSession.AntiforgeryToken? token =
      JsonSerializer.Deserialize<SignOutBrowserSession.AntiforgeryToken>(json, ContractSerializationDefaults.Options);
    token.ShouldNotBeNull();
    token.FormFieldName.ShouldNotBeNullOrWhiteSpace();
    token.RequestToken.ShouldNotBeNullOrWhiteSpace();
    return token;
  }

  private static async Task<bool> IsAuthenticatedAsync(HttpClient browser)
  {
    HttpResponseMessage response = await browser.GetAsync(GetCurrentSession.Query.RouteTemplate);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    GetCurrentSession.Response session =
      JsonSerializer.Deserialize<GetCurrentSession.Response>(json, ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("GetCurrentSession response deserialized to null.");
    return session.IsAuthenticated;
  }

  private static CookieContainer CreateJar(string sessionCookie)
  {
    string[] nameValue = sessionCookie.Split('=', 2);
    nameValue.Length.ShouldBe(2);
    CookieContainer jar = new();
    jar.Add(BaseUri, new Cookie(nameValue[0].Trim(), nameValue[1].Trim()));
    return jar;
  }

  private static bool HasSessionCookie(CookieContainer jar) =>
    jar.GetCookies(BaseUri).Any(cookie => cookie.Name == IdentitySessionDefaults.CookieName && !cookie.Expired);

  private static HttpClient CreateBrowser(CookieContainer jar)
  {
    HttpClientHandler handler = new()
    {
      AllowAutoRedirect = false,
      UseCookies = true,
      CookieContainer = jar,
      CheckCertificateRevocationList = true,
    };
    return new HttpClient(handler) { BaseAddress = BaseUri };
  }
}
