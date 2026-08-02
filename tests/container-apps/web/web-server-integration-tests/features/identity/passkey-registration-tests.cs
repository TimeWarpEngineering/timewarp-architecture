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
// CORRECTED (round-1 finding M6): WebTestServerApplication — and its HttpClient, and that
// HttpClient's cookie container — is constructed ONCE and shared across every test method in this
// class (Fixie's per-class fixture sharing; the sibling integration-software-authenticator.cs Design
// region documents the same observation, and it is precisely WHY that fixture's CredentialId had to
// become per-instance-random rather than a fixed constant — a fresh-per-method host could never have
// collided). Consequently the shared HttpClient's ambient cookie jar can carry a session cookie from
// an EARLIER test method into a LATER one. Every test here that asserts session state therefore
// isolates its own cookie explicitly (GetCurrentSessionWithCookie, using a fresh HttpClient scoped to
// just the Set-Cookie value this test's own completion response returned) rather than relying on the
// shared client's ambient state, so no test's session assertion depends on run order.
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

  private const string RpId = "localhost";
  private static TestApiService TestApiService => new(Web.HttpClient, ContractSerializationDefaults.Options);

  public static async Task Ok_With_Cookie_And_Session_Given_Valid_Registration()
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

    // Isolated cookie (see class Design region): proves THIS response's Set-Cookie actually
    // authenticates, independent of the shared HttpClient's ambient cookie jar.
    GetCurrentSession.Response sessionResponse = await GetCurrentSessionWithCookie(setCookieValues!);

    sessionResponse.IsAuthenticated.ShouldBeTrue();
    sessionResponse.PrincipalId.ShouldBe(completeResponse.PrincipalId);
  }

  public static async Task BadRequest_Given_Reused_Challenge()
  {
    CompletePasskeyRegistration.Command completeCommand = await BuildValidCompleteCommand(new IntegrationSoftwareAuthenticator());

    OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> first =
      await Web.GetResponse<CompletePasskeyRegistration.Response>(completeCommand, CancellationToken.None);
    first.IsT0.ShouldBeTrue("First completion should succeed.");

    // Replaying the exact same completed payload: the challenge was already consumed on the first
    // call (challenge-consume-before-verify is deliberate — see the handler's Design region), so this
    // must fail even though the payload itself is otherwise byte-identical to a valid one.
    OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> replay =
      await Web.GetResponse<CompletePasskeyRegistration.Response>(completeCommand, CancellationToken.None);

    replay.IsT2.ShouldBeTrue("Replayed completion should fail.");
    replay.AsT2.Status.ShouldBe(400);
  }

  public static async Task BadRequest_Given_Wrong_Origin()
  {
    IntegrationSoftwareAuthenticator authenticator = new();

    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);
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
      await Web.GetResponse<CompletePasskeyRegistration.Response>(completeCommand, CancellationToken.None);

    result.IsT2.ShouldBeTrue("Wrong-origin completion should fail.");
    result.AsT2.Status.ShouldBe(400);
  }

  public static async Task ValidationError_Given_Empty_CredentialId()
  {
    var command = new CompletePasskeyRegistration.Command
    {
      CredentialId = "",
      ClientDataJson = "QQ",
      AttestationObject = "QQ"
    };

    await Web.ConfirmEndpointValidationError<CompletePasskeyRegistration.Response>
      (command, nameof(CompletePasskeyRegistration.Command.CredentialId));
  }

  public static async Task ValidationError_Given_Oversized_CredentialId()
  {
    // Round-1 finding M4: CredentialId is now capped at 2KB — one character past the cap must
    // trigger the same validator rejection path as an empty field.
    var command = new CompletePasskeyRegistration.Command
    {
      CredentialId = new string('A', (2 * 1024) + 1),
      ClientDataJson = "QQ",
      AttestationObject = "QQ"
    };

    await Web.ConfirmEndpointValidationError<CompletePasskeyRegistration.Response>
      (command, nameof(CompletePasskeyRegistration.Command.CredentialId));
  }

  public static async Task Conflict_Given_Duplicate_Credential()
  {
    IntegrationSoftwareAuthenticator authenticator = new();

    CompletePasskeyRegistration.Command firstCommand = await BuildValidCompleteCommand(authenticator);
    OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> first =
      await Web.GetResponse<CompletePasskeyRegistration.Response>(firstCommand, CancellationToken.None);
    first.IsT0.ShouldBeTrue("First registration should succeed.");

    // Same authenticator instance (same per-instance CredentialId — see IntegrationSoftwareAuthenticator's
    // Design region), a brand-new ceremony/challenge.
    CompletePasskeyRegistration.Command secondCommand = await BuildValidCompleteCommand(authenticator);
    OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> second =
      await Web.GetResponse<CompletePasskeyRegistration.Response>(secondCommand, CancellationToken.None);

    second.IsT2.ShouldBeTrue("Duplicate credential registration should fail.");
    second.AsT2.Status.ShouldBe(409);
  }

  private static async Task<CompletePasskeyRegistration.Command> BuildValidCompleteCommand(IntegrationSoftwareAuthenticator authenticator)
  {
    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);

    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = Web.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

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

  // Isolated-cookie GetCurrentSession call (see class Design region, round-1 finding M6): a fresh
  // HttpClient carrying only the specific Set-Cookie value the caller captured, never the shared
  // Web.HttpClient's ambient cookie jar.
  private static async Task<GetCurrentSession.Response> GetCurrentSessionWithCookie(IEnumerable<string> setCookieValues)
  {
    string? sessionCookie = setCookieValues.FirstOrDefault
      (value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
    sessionCookie.ShouldNotBeNull("Expected a Set-Cookie header carrying the identity-session cookie.");

    using HttpClient isolatedClient = new() { BaseAddress = Web.HttpClient.BaseAddress };
    isolatedClient.DefaultRequestHeaders.Add("Cookie", sessionCookie.Split(';')[0]);

    HttpResponseMessage response = await isolatedClient.GetAsync(GetCurrentSession.Query.RouteTemplate);
    string json = await response.Content.ReadAsStringAsync();

    return JsonSerializer.Deserialize<GetCurrentSession.Response>(json, ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("GetCurrentSession response deserialized to null.");
  }

}
