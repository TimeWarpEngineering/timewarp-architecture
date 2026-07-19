#region Purpose
// End-to-end tests for the passkey registration ceremony (StartPasskeyRegistration +
// CompletePasskeyRegistration): real host, real cookie middleware, a deterministic software
// authenticator standing in for the browser/platform authenticator.
#endregion

#region Design
// These tests deliberately use WebTestServerApplication.GetResponse (real HTTP, through
// TestApiService/HttpClient), never .Send (ScopedSender — in-process, no HttpContext).
// CookieBrowserSessionService.IssueAsync requires an active HttpContext (it throws otherwise), so
// only the real-HTTP path can exercise a completing ceremony; .Send is what the Roles/TrackEvent
// suites use precisely because those handlers need no HttpContext.
// WebTestServerApplication.HttpClient is a single instance for the lifetime of the test (one fresh
// instance per test method, matching this project's established convention), and .NET's default
// HttpClientHandler carries its own cookie container — so a Set-Cookie from CompletePasskeyRegistration
// is automatically replayed on the following GetCurrentSession call within the same test, exactly
// like a real browser tab.
#endregion

namespace PasskeyRegistration_;

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

  public async Task Ok_With_Cookie_And_Session_Given_Valid_Registration()
  {
    CompletePasskeyRegistration.Command completeCommand = await BuildValidCompleteCommand(new IntegrationSoftwareAuthenticator());

    HttpResponseMessage httpResponse = await TestApiService.GetHttpResponseMessage(completeCommand, CancellationToken.None);

    httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    httpResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    setCookieValues.ShouldNotBeNull();
    setCookieValues.ShouldContain(value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));

    string json = await httpResponse.Content.ReadAsStringAsync();
    CompletePasskeyRegistration.Response? completeResponse =
      JsonSerializer.Deserialize<CompletePasskeyRegistration.Response>(json, ContractSerializationDefaults.Options);
    completeResponse.ShouldNotBeNull();

    OneOf<GetCurrentSession.Response, FileResponse, SharedProblemDetails> sessionResult =
      await WebTestServerApplication.GetResponse<GetCurrentSession.Response>(new GetCurrentSession.Query(), CancellationToken.None);

    sessionResult.AsT0.IsAuthenticated.ShouldBeTrue();
    sessionResult.AsT0.PrincipalId.ShouldBe(completeResponse.PrincipalId);
  }

  public async Task BadRequest_Given_Reused_Challenge()
  {
    CompletePasskeyRegistration.Command completeCommand = await BuildValidCompleteCommand(new IntegrationSoftwareAuthenticator());

    OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> first =
      await WebTestServerApplication.GetResponse<CompletePasskeyRegistration.Response>(completeCommand, CancellationToken.None);
    first.IsT0.ShouldBeTrue("First completion should succeed.");

    // Replaying the exact same completed payload: the challenge was already consumed on the first
    // call (challenge-consume-before-verify is deliberate — see the handler's Design region), so this
    // must fail even though the payload itself is otherwise byte-identical to a valid one.
    OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> replay =
      await WebTestServerApplication.GetResponse<CompletePasskeyRegistration.Response>(completeCommand, CancellationToken.None);

    replay.IsT2.ShouldBeTrue("Replayed completion should fail.");
    replay.AsT2.Status.ShouldBe(400);
  }

  public async Task BadRequest_Given_Wrong_Origin()
  {
    IntegrationSoftwareAuthenticator authenticator = new();

    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await WebTestServerApplication.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);
    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);

    byte[] authenticatorData = authenticator.BuildAuthenticatorData(RpId, includeAttestedCredentialData: true);
    byte[] attestationObject = IntegrationSoftwareAuthenticator.BuildAttestationObject(authenticatorData);
    byte[] clientDataJson = IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, "https://evil.example");

    var completeCommand = new CompletePasskeyRegistration.Command
    {
      CredentialId = Base64Url.EncodeToString(authenticator.CredentialId),
      ClientDataJson = Base64Url.EncodeToString(clientDataJson),
      AttestationObject = Base64Url.EncodeToString(attestationObject)
    };

    OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> result =
      await WebTestServerApplication.GetResponse<CompletePasskeyRegistration.Response>(completeCommand, CancellationToken.None);

    result.IsT2.ShouldBeTrue("Wrong-origin completion should fail.");
    result.AsT2.Status.ShouldBe(400);
  }

  public async Task ValidationError_Given_Empty_CredentialId()
  {
    var command = new CompletePasskeyRegistration.Command
    {
      CredentialId = "",
      ClientDataJson = "QQ",
      AttestationObject = "QQ"
    };

    await WebTestServerApplication.ConfirmEndpointValidationError<CompletePasskeyRegistration.Response>
      (command, nameof(CompletePasskeyRegistration.Command.CredentialId));
  }

  public async Task Conflict_Given_Duplicate_Credential()
  {
    IntegrationSoftwareAuthenticator authenticator = new();

    CompletePasskeyRegistration.Command firstCommand = await BuildValidCompleteCommand(authenticator);
    OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> first =
      await WebTestServerApplication.GetResponse<CompletePasskeyRegistration.Response>(firstCommand, CancellationToken.None);
    first.IsT0.ShouldBeTrue("First registration should succeed.");

    // Same authenticator (same fixed CredentialId), a brand-new ceremony/challenge.
    CompletePasskeyRegistration.Command secondCommand = await BuildValidCompleteCommand(authenticator);
    OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> second =
      await WebTestServerApplication.GetResponse<CompletePasskeyRegistration.Response>(secondCommand, CancellationToken.None);

    second.IsT2.ShouldBeTrue("Duplicate credential registration should fail.");
    second.AsT2.Status.ShouldBe(409);
  }

  private async Task<CompletePasskeyRegistration.Command> BuildValidCompleteCommand(IntegrationSoftwareAuthenticator authenticator)
  {
    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await WebTestServerApplication.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);

    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = WebTestServerApplication.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

    byte[] authenticatorData = authenticator.BuildAuthenticatorData(RpId, includeAttestedCredentialData: true);
    byte[] attestationObject = IntegrationSoftwareAuthenticator.BuildAttestationObject(authenticatorData);
    byte[] clientDataJson = IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, origin);

    return new CompletePasskeyRegistration.Command
    {
      CredentialId = Base64Url.EncodeToString(authenticator.CredentialId),
      ClientDataJson = Base64Url.EncodeToString(clientDataJson),
      AttestationObject = Base64Url.EncodeToString(attestationObject)
    };
  }

  private static byte[] ReadChallenge(string optionsJson)
  {
    using JsonDocument document = JsonDocument.Parse(optionsJson);
    string challengeBase64Url = document.RootElement.GetProperty("challenge").GetString()!;
    return Base64Url.DecodeFromChars(challengeBase64Url);
  }
}
