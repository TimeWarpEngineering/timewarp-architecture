#region Purpose
// Integration tests for EndBrowserSession: register passkey → cookie → POST session/end → anonymous.
#endregion

#region Design
// Mirrors passkey-registration-tests isolation of Set-Cookie (shared HostGraph HttpClient jar).
// Task 104-034: SPA sign-out depends on this endpoint clearing the identity-session cookie.
#endregion

namespace EndBrowserSession_;

using System.Buffers.Text;
using System.Net;
using System.Text.Json;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;

public class Ends_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Ends_>();

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

  private const string RpId = "localhost";
  private static TestApiService TestApiService => new(Web.HttpClient, ContractSerializationDefaults.Options);

  public static async Task Session_Then_Anonymous_After_End()
  {
    CompletePasskeyRegistration.Command completeCommand =
      await BuildValidCompleteCommand(new IntegrationSoftwareAuthenticator());

    HttpResponseMessage completeHttp =
      await TestApiService.GetHttpResponseMessage(completeCommand, CancellationToken.None);
    completeHttp.StatusCode.ShouldBe(HttpStatusCode.OK);
    completeHttp.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues)
      .ShouldBeTrue();

    GetCurrentSession.Response before = await GetCurrentSessionWithCookie(setCookieValues!);
    before.IsAuthenticated.ShouldBeTrue();
    before.PrincipalId.ShouldNotBeNull();

    // CookieContainer applies SignOutAsync's expired Set-Cookie (browser behavior). Re-sending
    // the pre-logout Cookie header alone would still present a valid encrypted ticket.
    using HttpClient isolated = CreateClientWithCookieContainer(setCookieValues!);
    HttpResponseMessage endResponse = await isolated.PostAsync(
      EndBrowserSession.Command.RouteTemplate,
      content: null);
    endResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

    HttpResponseMessage sessionAfter = await isolated.GetAsync(GetCurrentSession.Query.RouteTemplate);
    sessionAfter.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await sessionAfter.Content.ReadAsStringAsync();
    GetCurrentSession.Response after =
      JsonSerializer.Deserialize<GetCurrentSession.Response>(json, ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("null session");
    after.IsAuthenticated.ShouldBeFalse();
    after.PrincipalId.ShouldBeNull();
  }

  public static async Task Idempotent_When_Already_Anonymous()
  {
    OneOf<EndBrowserSession.Response, FileResponse, SharedProblemDetails> result =
      await Web.GetResponse<EndBrowserSession.Response>(
        new EndBrowserSession.Command(),
        CancellationToken.None);

    result.IsT0.ShouldBeTrue("End session without cookie must succeed.");
  }

  private static async Task<CompletePasskeyRegistration.Command> BuildValidCompleteCommand(
    IntegrationSoftwareAuthenticator authenticator)
  {
    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartPasskeyRegistration.Response>(
        new StartPasskeyRegistration.Command(),
        CancellationToken.None);

    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = Web.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

    byte[] authenticatorData = authenticator.BuildAuthenticatorData(RpId, includeAttestedCredentialData: true);
    byte[] attestationObject = IntegrationSoftwareAuthenticator.BuildAttestationObject(authenticatorData);
    byte[] clientDataJson = IntegrationSoftwareAuthenticator.BuildClientDataJson(
      "webauthn.create",
      challenge,
      origin);

    return new CompletePasskeyRegistration.Command
    {
      CredentialId = Base64Url.EncodeToString(authenticator.CredentialId),
      ClientDataJson = Base64Url.EncodeToString(clientDataJson),
      AttestationObject = Base64Url.EncodeToString(attestationObject),
    };
  }

  private static byte[] ReadChallenge(string optionsJson)
  {
    using JsonDocument document = JsonDocument.Parse(optionsJson);
    string challengeBase64Url = document.RootElement.GetProperty("challenge").GetString()!;
    return Base64Url.DecodeFromChars(challengeBase64Url);
  }

  private static async Task<GetCurrentSession.Response> GetCurrentSessionWithCookie(
    IEnumerable<string> setCookieValues)
  {
    using HttpClient isolatedClient = CreateClientWithCookieHeader(setCookieValues);
    HttpResponseMessage response = await isolatedClient.GetAsync(GetCurrentSession.Query.RouteTemplate);
    string json = await response.Content.ReadAsStringAsync();

    return JsonSerializer.Deserialize<GetCurrentSession.Response>(json, ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("GetCurrentSession response deserialized to null.");
  }

  private static HttpClient CreateClientWithCookieHeader(IEnumerable<string> setCookieValues)
  {
    string? sessionCookie = setCookieValues.FirstOrDefault(
      value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
    sessionCookie.ShouldNotBeNull("Expected a Set-Cookie header carrying the identity-session cookie.");

    HttpClient isolatedClient = new() { BaseAddress = Web.HttpClient.BaseAddress };
    isolatedClient.DefaultRequestHeaders.Add("Cookie", sessionCookie.Split(';')[0]);
    return isolatedClient;
  }

  private static HttpClient CreateClientWithCookieContainer(IEnumerable<string> setCookieValues)
  {
    string? sessionCookie = setCookieValues.FirstOrDefault(
      value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
    sessionCookie.ShouldNotBeNull("Expected a Set-Cookie header carrying the identity-session cookie.");

    Uri baseUri = Web.HttpClient.BaseAddress
      ?? throw new InvalidOperationException("Web HttpClient BaseAddress is null.");
    var container = new CookieContainer();
    string nameValue = sessionCookie.Split(';')[0];
    string[] nv = nameValue.Split('=', 2);
    nv.Length.ShouldBe(2);
    container.Add(baseUri, new Cookie(nv[0].Trim(), nv[1].Trim()));

    var handler = new HttpClientHandler
    {
      UseCookies = true,
      CookieContainer = container,
      CheckCertificateRevocationList = true,
    };
    return new HttpClient(handler) { BaseAddress = baseUri };
  }
}
