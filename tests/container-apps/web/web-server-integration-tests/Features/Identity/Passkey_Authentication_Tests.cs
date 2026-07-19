#region Purpose
// End-to-end tests for the passkey authentication ceremony (StartPasskeyAuthentication +
// CompletePasskeyAuthentication): real host, real cookie middleware, register-then-authenticate
// against the same deterministic software authenticator used by the registration suite.
#endregion

#region Design
// Same GetResponse-not-Send rationale as Passkey_Registration_Tests.cs — CompletePasskeyAuthentication
// also issues a cookie via CookieBrowserSessionService, which requires a real HttpContext.
#endregion

namespace PasskeyAuthentication_;

using System.Buffers.Text;
using System.Net;
using System.Text.Json;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;

public class Returns_
{
  private const string RpId = "localhost";

  private readonly WebTestServerApplication WebTestServerApplication;
  private readonly TestApiService TestApiService;

  public Returns_(WebTestServerApplication webTestServerApplication)
  {
    WebTestServerApplication = webTestServerApplication;
    TestApiService = new TestApiService(webTestServerApplication.HttpClient, ContractSerializationDefaults.Options);
  }

  public async Task Ok_With_Cookie_And_Session_Given_Valid_Authentication()
  {
    IntegrationSoftwareAuthenticator authenticator = new();
    PrincipalId registeredPrincipalId = await RegisterPasskey(authenticator);

    CompletePasskeyAuthentication.Command authenticateCommand = await BuildValidAuthenticateCommand(authenticator);

    HttpResponseMessage httpResponse = await TestApiService.GetHttpResponseMessage(authenticateCommand, CancellationToken.None);

    httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    httpResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    setCookieValues.ShouldNotBeNull();
    setCookieValues.ShouldContain(value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));

    string json = await httpResponse.Content.ReadAsStringAsync();
    CompletePasskeyAuthentication.Response? authenticateResponse =
      JsonSerializer.Deserialize<CompletePasskeyAuthentication.Response>(json, ContractSerializationDefaults.Options);
    authenticateResponse.ShouldNotBeNull();
    authenticateResponse.PrincipalId.ShouldBe(registeredPrincipalId);

    OneOf<GetCurrentSession.Response, FileResponse, SharedProblemDetails> sessionResult =
      await WebTestServerApplication.GetResponse<GetCurrentSession.Response>(new GetCurrentSession.Query(), CancellationToken.None);

    sessionResult.AsT0.IsAuthenticated.ShouldBeTrue();
    sessionResult.AsT0.PrincipalId.ShouldBe(registeredPrincipalId);
  }

  public async Task BadRequest_Given_Unknown_Credential()
  {
    // Never registered — FindCredentialByHandleAsync must return null.
    IntegrationSoftwareAuthenticator authenticator = new();
    CompletePasskeyAuthentication.Command authenticateCommand = await BuildValidAuthenticateCommand(authenticator);

    OneOf<CompletePasskeyAuthentication.Response, FileResponse, SharedProblemDetails> result =
      await WebTestServerApplication.GetResponse<CompletePasskeyAuthentication.Response>(authenticateCommand, CancellationToken.None);

    result.IsT2.ShouldBeTrue("Authentication with an unregistered credential should fail.");
    result.AsT2.Status.ShouldBe(400);
  }

  public async Task BadRequest_Given_Tampered_Signature()
  {
    IntegrationSoftwareAuthenticator authenticator = new();
    await RegisterPasskey(authenticator);

    CompletePasskeyAuthentication.Command authenticateCommand = await BuildValidAuthenticateCommand(authenticator);

    byte[] signature = Base64Url.DecodeFromChars(authenticateCommand.Signature);
    signature[0] ^= 0xFF;
    authenticateCommand.Signature = Base64Url.EncodeToString(signature);

    OneOf<CompletePasskeyAuthentication.Response, FileResponse, SharedProblemDetails> result =
      await WebTestServerApplication.GetResponse<CompletePasskeyAuthentication.Response>(authenticateCommand, CancellationToken.None);

    result.IsT2.ShouldBeTrue("Authentication with a tampered signature should fail.");
    result.AsT2.Status.ShouldBe(400);
  }

  public async Task BadRequest_Given_Reused_Challenge()
  {
    IntegrationSoftwareAuthenticator authenticator = new();
    await RegisterPasskey(authenticator);

    CompletePasskeyAuthentication.Command authenticateCommand = await BuildValidAuthenticateCommand(authenticator);

    OneOf<CompletePasskeyAuthentication.Response, FileResponse, SharedProblemDetails> first =
      await WebTestServerApplication.GetResponse<CompletePasskeyAuthentication.Response>(authenticateCommand, CancellationToken.None);
    first.IsT0.ShouldBeTrue("First authentication should succeed.");

    OneOf<CompletePasskeyAuthentication.Response, FileResponse, SharedProblemDetails> replay =
      await WebTestServerApplication.GetResponse<CompletePasskeyAuthentication.Response>(authenticateCommand, CancellationToken.None);

    replay.IsT2.ShouldBeTrue("Replayed authentication should fail.");
    replay.AsT2.Status.ShouldBe(400);
  }

  private async Task<PrincipalId> RegisterPasskey(IntegrationSoftwareAuthenticator authenticator)
  {
    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await WebTestServerApplication.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);

    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = WebTestServerApplication.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

    byte[] authenticatorData = authenticator.BuildAuthenticatorData(RpId, includeAttestedCredentialData: true);
    byte[] attestationObject = IntegrationSoftwareAuthenticator.BuildAttestationObject(authenticatorData);
    byte[] clientDataJson = IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, origin);

    var registerCommand = new CompletePasskeyRegistration.Command
    {
      CredentialId = Base64Url.EncodeToString(authenticator.CredentialId),
      ClientDataJson = Base64Url.EncodeToString(clientDataJson),
      AttestationObject = Base64Url.EncodeToString(attestationObject)
    };

    OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> result =
      await WebTestServerApplication.GetResponse<CompletePasskeyRegistration.Response>(registerCommand, CancellationToken.None);

    result.IsT0.ShouldBeTrue("Registration setup for an authentication test should succeed.");
    return result.AsT0.PrincipalId;
  }

  private async Task<CompletePasskeyAuthentication.Command> BuildValidAuthenticateCommand(IntegrationSoftwareAuthenticator authenticator)
  {
    OneOf<StartPasskeyAuthentication.Response, FileResponse, SharedProblemDetails> start =
      await WebTestServerApplication.GetResponse<StartPasskeyAuthentication.Response>(new StartPasskeyAuthentication.Command(), CancellationToken.None);

    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = WebTestServerApplication.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

    // Assertions carry no attested credential data — that block is registration-only.
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(RpId, includeAttestedCredentialData: false);
    byte[] clientDataJson = IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.get", challenge, origin);
    byte[] signature = authenticator.Sign(authenticatorData, clientDataJson);

    return new CompletePasskeyAuthentication.Command
    {
      CredentialId = Base64Url.EncodeToString(authenticator.CredentialId),
      ClientDataJson = Base64Url.EncodeToString(clientDataJson),
      AuthenticatorData = Base64Url.EncodeToString(authenticatorData),
      Signature = Base64Url.EncodeToString(signature)
    };
  }

  private static byte[] ReadChallenge(string optionsJson)
  {
    using JsonDocument document = JsonDocument.Parse(optionsJson);
    string challengeBase64Url = document.RootElement.GetProperty("challenge").GetString()!;
    return Base64Url.DecodeFromChars(challengeBase64Url);
  }
}
