#region Purpose
// End-to-end tests for per-request WebAuthn RP-ID selection (task 104-031): the same running host
// serves passkeys under a second allowlisted host, rejects an unlisted host, and ignores a spoofed
// X-Forwarded-Host — the RP ID is chosen from the real request Host against WebAuthnOptions.AllowedRpIds.
#endregion

#region Design
// The test project's appsettings.json pins AllowedRpIds to ["localhost","webauthn-second.test"]
// (localhost is the C# default; webauthn-second.test is appended — see WebAuthnOptions_Binding_Tests).
// Each request sets its Host explicitly via HttpRequestMessage.Headers.Host so a single shared host
// (bound to localhost:7000) exercises multiple RP IDs. Raw HttpRequestMessage (not TestApiService)
// is required because per-request Host cannot be set through the shared HttpClient's default headers.
// The ceremony vectors are built with rpId/origin matching the SELECTED host: registering under
// webauthn-second.test means authenticatorData hashes "webauthn-second.test" and clientDataJSON's
// origin is https://webauthn-second.test — the empty-AllowedOrigins fallback then accepts it because
// its host equals the selected RP ID.
// X-Forwarded-Host is asserted to have NO effect: selection reads HttpContext.Request.Host only (the
// ingress preserves the ORIGINAL Host; no UseForwardedHeaders consumes a spoofable forwarded header),
// so a forged X-Forwarded-Host can never move selection off the real Host — see the AppHost's Design
// region.
#endregion

namespace PasskeyHostSelection_;

using System.Buffers.Text;
using System.Net;
using System.Text;
using System.Text.Json;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;

public class Returns_
{
  private const string SecondHost = "webauthn-second.test";
  private const string SecondOrigin = "https://webauthn-second.test";
  private const string UnlistedHost = "not-allowed.example";

  private readonly WebTestServerApplication WebTestServerApplication;

  public Returns_(WebTestServerApplication webTestServerApplication)
  {
    WebTestServerApplication = webTestServerApplication;
  }

  public async Task Ok_Register_And_Authenticate_Under_Second_Allowed_Host()
  {
    IntegrationSoftwareAuthenticator authenticator = new();

    // Register a passkey scoped to the second allowlisted RP ID.
    byte[] registerChallenge = await StartCeremony(StartPasskeyRegistration.Command.RouteTemplate, new StartPasskeyRegistration.Command(), SecondHost);

    byte[] registerAuthData = authenticator.BuildAuthenticatorData(SecondHost, includeAttestedCredentialData: true);
    byte[] attestationObject = IntegrationSoftwareAuthenticator.BuildAttestationObject(registerAuthData);
    byte[] registerClientData = IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.create", registerChallenge, SecondOrigin);

    var completeRegister = new CompletePasskeyRegistration.Command
    {
      CredentialId = Base64Url.EncodeToString(authenticator.CredentialId),
      ClientDataJson = Base64Url.EncodeToString(registerClientData),
      AttestationObject = Base64Url.EncodeToString(attestationObject)
    };

    HttpResponseMessage registerResponse = await Post(CompletePasskeyRegistration.Command.RouteTemplate, completeRegister, SecondHost);
    registerResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

    // Authenticate the same passkey, still under the second host.
    byte[] authChallenge = await StartCeremony(StartPasskeyAuthentication.Command.RouteTemplate, new StartPasskeyAuthentication.Command(), SecondHost);

    byte[] assertAuthData = authenticator.BuildAuthenticatorData(SecondHost, includeAttestedCredentialData: false);
    byte[] assertClientData = IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.get", authChallenge, SecondOrigin);
    byte[] signature = authenticator.Sign(assertAuthData, assertClientData);

    var completeAuthenticate = new CompletePasskeyAuthentication.Command
    {
      CredentialId = Base64Url.EncodeToString(authenticator.CredentialId),
      ClientDataJson = Base64Url.EncodeToString(assertClientData),
      AuthenticatorData = Base64Url.EncodeToString(assertAuthData),
      Signature = Base64Url.EncodeToString(signature)
    };

    HttpResponseMessage authenticateResponse = await Post(CompletePasskeyAuthentication.Command.RouteTemplate, completeAuthenticate, SecondHost);
    authenticateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
  }

  public async Task BadRequest_StartRegistration_Given_Unlisted_Host()
  {
    HttpResponseMessage response = await Post(StartPasskeyRegistration.Command.RouteTemplate, new StartPasskeyRegistration.Command(), UnlistedHost);

    await ShouldBeHostNotAllowed(response);
  }

  public async Task BadRequest_StartAuthentication_Given_Unlisted_Host()
  {
    HttpResponseMessage response = await Post(StartPasskeyAuthentication.Command.RouteTemplate, new StartPasskeyAuthentication.Command(), UnlistedHost);

    await ShouldBeHostNotAllowed(response);
  }

  public async Task Selection_Stays_Localhost_Given_Spoofed_XForwardedHost()
  {
    // Real Host is localhost; an attacker sets X-Forwarded-Host to another allowlisted host. Selection
    // must read the real Host only, so the minted options' rp.id stays "localhost".
    HttpResponseMessage response = await Post
    (
      StartPasskeyRegistration.Command.RouteTemplate,
      new StartPasskeyRegistration.Command(),
      host: "localhost",
      forwardedHost: SecondHost
    );

    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    StartPasskeyRegistration.Response? startResponse =
      JsonSerializer.Deserialize<StartPasskeyRegistration.Response>(await response.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    startResponse.ShouldNotBeNull();

    using JsonDocument optionsDocument = JsonDocument.Parse(startResponse.OptionsJson);
    string rpId = optionsDocument.RootElement.GetProperty("rp").GetProperty("id").GetString()!;

    rpId.ShouldBe("localhost");
  }

  private async Task<byte[]> StartCeremony<TCommand>(string routeTemplate, TCommand command, string host)
    where TCommand : class
  {
    HttpResponseMessage response = await Post(routeTemplate, command, host);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    string body = await response.Content.ReadAsStringAsync();
    string optionsJson = JsonDocument.Parse(body).RootElement.GetProperty("optionsJson").GetString()!;
    return ReadChallenge(optionsJson);
  }

  private async Task<HttpResponseMessage> Post<TCommand>(string routeTemplate, TCommand command, string host, string? forwardedHost = null)
  {
    string json = JsonSerializer.Serialize(command, ContractSerializationDefaults.Options);

    // Setting Headers.Host makes SocketsHttpHandler use it as the TLS SNI/target host, so the
    // localhost dev cert no longer name-matches when Host is a share hostname. The TCP connection
    // still targets localhost:7000 (the request URI's authority); accept the dev cert regardless of
    // the mismatched target name — this test is exercising RP-ID selection, not TLS.
    using var handler = new HttpClientHandler
    {
      CheckCertificateRevocationList = true,
      ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
    using var client = new HttpClient(handler) { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };

    using var request = new HttpRequestMessage(HttpMethod.Post, routeTemplate)
    {
      Content = new StringContent(json, Encoding.UTF8, "application/json")
    };
    request.Headers.Host = host;
    if (forwardedHost is not null)
    {
      request.Headers.Add("X-Forwarded-Host", forwardedHost);
    }

    return await client.SendAsync(request);
  }

  private static async Task ShouldBeHostNotAllowed(HttpResponseMessage response)
  {
    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

    SharedProblemDetails? problem =
      JsonSerializer.Deserialize<SharedProblemDetails>(await response.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    problem.ShouldNotBeNull();
    problem.Status.ShouldBe(400);
    problem.Title.ShouldBe("Host not allowed");
  }

  private static byte[] ReadChallenge(string optionsJson)
  {
    using JsonDocument document = JsonDocument.Parse(optionsJson);
    string challengeBase64Url = document.RootElement.GetProperty("challenge").GetString()!;
    return Base64Url.DecodeFromChars(challengeBase64Url);
  }
}
