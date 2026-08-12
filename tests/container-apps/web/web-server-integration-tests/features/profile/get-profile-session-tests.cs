#region Purpose
// Regression coverage for task 150: an authorized identity-session cookie must resolve GetProfile
// to a real store-backed profile, not the anonymous demo mock; an anonymous request must still get
// the mock.
#endregion

#region Design
// Root cause (task 150): the identity-session cookie principal carries only the
// timewarp:principal_id claim (cookie-browser-session-service-server.cs /
// identity-session-defaults-server.cs); foundation's ICurrentUserService reads a "UserId" claim
// that no scheme ever emits, so GetProfile.Handler always fell through to the anonymous mock for
// authenticated callers. The fix swaps the handler to ICurrentPrincipalAccessor. The
// RealProfile_Given_Authorized_Session test below fails without that fix — see task 150's Results
// for the observed failing assertion text.
// Register+authenticate+isolated-cookie pattern is modeled directly on
// features/identity/passkey-authentication-tests.cs's Returns_.Ok_With_Cookie_And_Session_Given_
// Valid_Authentication / GetCurrentSessionWithCookie (same reason: the shared HttpClient's ambient
// cookie jar must not be relied on — each test isolates its own session cookie).
// Fixture: C-create per class (AGENTS.md default, 145-008) — SetupOnce/CleanUpOnce own a fresh
// HostGraph, same as Passkey_Authentication_Tests.cs; this suite is not expensive/multi-class
// enough to warrant opting into the session fixture.
#endregion

namespace GetProfileSession_;

using System.Buffers.Text;
using System.Net;
using System.Text.Json;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.Profiles;
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

  public static async Task RealProfile_Given_Authorized_Session()
  {
    IntegrationSoftwareAuthenticator authenticator = new();
    await RegisterPasskey(authenticator);

    CompletePasskeyAuthentication.Command authenticateCommand = await BuildValidAuthenticateCommand(authenticator);
    HttpResponseMessage authResponse = await TestApiService.GetHttpResponseMessage(authenticateCommand, CancellationToken.None);

    authResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    authResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    setCookieValues.ShouldNotBeNull();

    GetProfile.Response profileResponse = await GetProfileWithCookie(setCookieValues!);

    // Task 150 regression: before the fix, an authorized session still resolved to the anonymous
    // contract mock (Alias "alias") because ICurrentUserService's "UserId" claim is never issued.
    profileResponse.Alias.ShouldBe("Member", "Task 150: authorized session must get the store-backed create-if-missing profile, not the anonymous mock.");
    profileResponse.Alias.ShouldNotBe("alias", "Task 150: authorized session must not fall through to the anonymous contract mock.");
    profileResponse.Avatar.ShouldStartWith("data:image/svg+xml;base64,");
  }

  public static async Task AnonymousMock_Given_No_Session()
  {
    // Isolated client, no cookie jar (see class Design region): the shared Web.HttpClient's
    // ambient cookie container can carry a session cookie left over from another test method in
    // this class (e.g. RealProfile_Given_Authorized_Session's Set-Cookie), so this test must not
    // reuse it — an anonymous request must go out with no cookie at all.
    using HttpClient anonymousClient = new() { BaseAddress = Web.HttpClient.BaseAddress };
    HttpResponseMessage response = await anonymousClient.GetAsync(GetProfile.Query.RouteTemplate);
    string json = await response.Content.ReadAsStringAsync();

    GetProfile.Response? profileResponse =
      JsonSerializer.Deserialize<GetProfile.Response>(json, ContractSerializationDefaults.Options);

    profileResponse.ShouldNotBeNull();
    profileResponse.Alias.ShouldBe("alias");
  }

  private static async Task<PrincipalId> RegisterPasskey(IntegrationSoftwareAuthenticator authenticator)
  {
    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);

    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = Web.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

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
      await Web.GetResponse<CompletePasskeyRegistration.Response>(registerCommand, CancellationToken.None);

    result.IsT0.ShouldBeTrue("Registration setup for a profile-session test should succeed.");
    return result.AsT0.PrincipalId;
  }

  private static async Task<CompletePasskeyAuthentication.Command> BuildValidAuthenticateCommand(IntegrationSoftwareAuthenticator authenticator)
  {
    OneOf<StartPasskeyAuthentication.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartPasskeyAuthentication.Response>(new StartPasskeyAuthentication.Command(), CancellationToken.None);

    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = Web.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

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

  // Isolated-cookie GET (see class Design region) — mirrors Passkey_Authentication_Tests.cs's
  // GetCurrentSessionWithCookie, targeting GetProfile instead of GetCurrentSession.
  private static async Task<GetProfile.Response> GetProfileWithCookie(IEnumerable<string> setCookieValues)
  {
    string? sessionCookie = setCookieValues.FirstOrDefault
      (value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
    sessionCookie.ShouldNotBeNull("Expected a Set-Cookie header carrying the identity-session cookie.");

    using HttpClient isolatedClient = new() { BaseAddress = Web.HttpClient.BaseAddress };
    isolatedClient.DefaultRequestHeaders.Add("Cookie", sessionCookie.Split(';')[0]);

    HttpResponseMessage response = await isolatedClient.GetAsync(GetProfile.Query.RouteTemplate);
    string json = await response.Content.ReadAsStringAsync();

    return JsonSerializer.Deserialize<GetProfile.Response>(json, ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("GetProfile response deserialized to null.");
  }

}
