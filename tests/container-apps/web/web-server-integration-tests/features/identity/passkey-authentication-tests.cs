#region Purpose
// End-to-end tests for the passkey authentication ceremony (StartPasskeyAuthentication +
// CompletePasskeyAuthentication): real host, real cookie middleware, register-then-authenticate
// against the same deterministic software authenticator used by the registration suite.
#endregion

#region Design
// Same GetResponse-not-Send rationale as Passkey_Registration_Tests.cs — CompletePasskeyAuthentication
// also issues a cookie via CookieBrowserSessionService, which requires a real HttpContext.
// Same per-class fixture-sharing caveat as Passkey_Registration_Tests.cs's Design region (round-1
// finding M6) also applies here: WebTestServerApplication/HttpClient/cookie-jar are shared across
// every test method in this class, so the happy-path test below isolates its own session cookie
// (GetCurrentSessionWithCookie) rather than relying on the shared client's ambient state.
#endregion

namespace PasskeyAuthentication_;

using System.Buffers.Text;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
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

  public static async Task Ok_With_Cookie_And_Session_Given_Valid_Authentication()
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

    // Isolated cookie (see class Design region): proves THIS response's Set-Cookie actually
    // authenticates, independent of the shared HttpClient's ambient cookie jar.
    GetCurrentSession.Response sessionResponse = await GetCurrentSessionWithCookie(setCookieValues!);

    sessionResponse.IsAuthenticated.ShouldBeTrue();
    sessionResponse.PrincipalId.ShouldBe(registeredPrincipalId);
  }

  public static async Task BadRequest_Given_Unknown_Credential()
  {
    // Never registered — FindCredentialByHandleAsync must return null.
    IntegrationSoftwareAuthenticator authenticator = new();
    CompletePasskeyAuthentication.Command authenticateCommand = await BuildValidAuthenticateCommand(authenticator);

    OneOf<CompletePasskeyAuthentication.Response, FileResponse, SharedProblemDetails> result =
      await Web.GetResponse<CompletePasskeyAuthentication.Response>(authenticateCommand, CancellationToken.None);

    result.IsT2.ShouldBeTrue("Authentication with an unregistered credential should fail.");
    result.AsT2.Status.ShouldBe(400);
  }

  public static async Task BadRequest_Given_Tampered_Signature()
  {
    IntegrationSoftwareAuthenticator authenticator = new();
    await RegisterPasskey(authenticator);

    CompletePasskeyAuthentication.Command authenticateCommand = await BuildValidAuthenticateCommand(authenticator);

    byte[] signature = Base64Url.DecodeFromChars(authenticateCommand.Signature);
    signature[0] ^= 0xFF;
    authenticateCommand.Signature = Base64Url.EncodeToString(signature);

    OneOf<CompletePasskeyAuthentication.Response, FileResponse, SharedProblemDetails> result =
      await Web.GetResponse<CompletePasskeyAuthentication.Response>(authenticateCommand, CancellationToken.None);

    result.IsT2.ShouldBeTrue("Authentication with a tampered signature should fail.");
    result.AsT2.Status.ShouldBe(400);
  }

  public static async Task BadRequest_Given_Reused_Challenge()
  {
    IntegrationSoftwareAuthenticator authenticator = new();
    await RegisterPasskey(authenticator);

    CompletePasskeyAuthentication.Command authenticateCommand = await BuildValidAuthenticateCommand(authenticator);

    OneOf<CompletePasskeyAuthentication.Response, FileResponse, SharedProblemDetails> first =
      await Web.GetResponse<CompletePasskeyAuthentication.Response>(authenticateCommand, CancellationToken.None);
    first.IsT0.ShouldBeTrue("First authentication should succeed.");

    OneOf<CompletePasskeyAuthentication.Response, FileResponse, SharedProblemDetails> replay =
      await Web.GetResponse<CompletePasskeyAuthentication.Response>(authenticateCommand, CancellationToken.None);

    replay.IsT2.ShouldBeTrue("Replayed authentication should fail.");
    replay.AsT2.Status.ShouldBe(400);
  }

  public static async Task ValidationError_Given_Oversized_CredentialId()
  {
    // Round-1 finding M4: CredentialId is now capped at 2KB on this command too.
    var command = new CompletePasskeyAuthentication.Command
    {
      CredentialId = new string('A', (2 * 1024) + 1),
      ClientDataJson = "QQ",
      AuthenticatorData = "QQ",
      Signature = "QQ"
    };

    await Web.ConfirmEndpointValidationError<CompletePasskeyAuthentication.Response>
      (command, nameof(CompletePasskeyAuthentication.Command.CredentialId));
  }

  public static async Task Forbidden_Given_Quarantined_Principal()
  {
    // G3 (104-006): crypto must succeed, then quarantine is the distinct 403 signal — not 400.
    IntegrationSoftwareAuthenticator authenticator = new();
    PrincipalId principalId = await RegisterPasskey(authenticator);
    await QuarantinePrincipal(principalId);

    CompletePasskeyAuthentication.Command authenticateCommand = await BuildValidAuthenticateCommand(authenticator);

    OneOf<CompletePasskeyAuthentication.Response, FileResponse, SharedProblemDetails> result =
      await Web.GetResponse<CompletePasskeyAuthentication.Response>(authenticateCommand, CancellationToken.None);

    result.IsT2.ShouldBeTrue("Authentication of a quarantined principal should return a problem.");
    result.AsT2.Status.ShouldBe(403);
    result.AsT2.Title.ShouldBe("Account quarantined");
  }

  private static async Task QuarantinePrincipal(PrincipalId principalId)
  {
    IPrincipalStore store =
      Web.WebApplicationHost.ServiceProvider.GetRequiredService<IPrincipalStore>();
    Principal? principal = await store.GetPrincipalAsync(principalId);
    principal.ShouldNotBeNull();
    principal.Quarantine();
    await store.UpdatePrincipalAsync(principal);
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

    result.IsT0.ShouldBeTrue("Registration setup for an authentication test should succeed.");
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

  // Isolated-cookie GetCurrentSession call (see class Design region, round-1 finding M6) — see
  // Passkey_Registration_Tests.cs's identical helper for the full rationale.
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
