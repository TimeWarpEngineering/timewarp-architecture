#region Purpose
// End-to-end tests for the ONE bearer-protected endpoint this task ships (GetAgentIdentity,
// policy AgentTokenDefaults.IdentityReadPolicy): proves bearer validation, scope enforcement, and —
// the standing scheme-isolation caution — that a cookie session cannot reach it.
#endregion

#region Design
// Each test builds its own isolated HttpClient (never relies on WebTestServerApplication.HttpClient's
// shared ambient cookie jar/headers — same isolation posture as the round-1 fix in
// Passkey_Registration_Tests.cs/Passkey_Authentication_Tests.cs, applied from day one here) so
// Authorization/Cookie headers set by one test can never leak into another sharing this class's
// fixture (Fixie per-class WebTestServerApplication sharing).
#endregion

namespace AgentProtectedEndpoint_;

using System.Buffers.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;

public class Returns_
{
  private readonly WebTestServerApplication WebTestServerApplication;

  public Returns_(WebTestServerApplication webTestServerApplication)
  {
    WebTestServerApplication = webTestServerApplication;
  }

  public async Task Ok_Given_Valid_IdentityRead_Token()
  {
    var key = new IntegrationSoftwareAgentKey();
    string accessToken = await RegisterAndIssueToken(key, [AgentScopes.IdentityRead]);

    HttpResponseMessage response = await GetAgentMeWithBearer(accessToken);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    // Server must emit PascalCase string enums (ContractSerializationDefaults on MVC JsonOptions), not integers.
    json.ShouldContain("\"kind\":\"Agent\"");
    json.ShouldContain("\"trustTier\":\"Keyed\"");
    json.ShouldNotContain("\"kind\":2");
    json.ShouldNotContain("\"trustTier\":2");
    GetAgentIdentity.Response? parsed = JsonSerializer.Deserialize<GetAgentIdentity.Response>(json, ContractSerializationDefaults.Options);
    parsed.ShouldNotBeNull();
    parsed.Kind.ShouldBe(PrincipalKind.Agent);
    parsed.TrustTier.ShouldBe(TrustTier.Keyed);
    parsed.Scopes.ShouldContain(AgentScopes.IdentityRead);
  }

  public async Task Unauthorized_Given_No_Header()
  {
    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };

    HttpResponseMessage response = await client.GetAsync(GetAgentIdentity.Query.RouteTemplate);

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    response.Headers.GetValues("WWW-Authenticate").Single().ShouldBe("Bearer");
    response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
  }

  public async Task Unauthorized_InvalidToken_Given_Garbage_Bearer()
  {
    HttpResponseMessage response = await GetAgentMeWithBearer("not-a-real-token");

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    response.Headers.GetValues("WWW-Authenticate").Single().ShouldBe("Bearer error=\"invalid_token\"");
  }

  public async Task Forbidden_InsufficientScope_Given_DemoInvoke_Only_Token()
  {
    var key = new IntegrationSoftwareAgentKey();
    string accessToken = await RegisterAndIssueToken(key, [AgentScopes.DemoInvoke]);

    HttpResponseMessage response = await GetAgentMeWithBearer(accessToken);

    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    response.Headers.GetValues("WWW-Authenticate").Single().ShouldBe("Bearer error=\"insufficient_scope\"");
  }

  public async Task Unauthorized_Given_CookieSession_Only_No_Bearer()
  {
    // Scheme isolation (task 104-004 §6 standing caution): a valid identity-session COOKIE (from the
    // 104-003 passkey flow) must not reach this bearer-scheme-restricted policy.
    var authenticator = new IntegrationSoftwareAuthenticator();

    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await WebTestServerApplication.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);
    byte[] challenge = ReadWebAuthnChallenge(start.AsT0.OptionsJson);
    string origin = WebTestServerApplication.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

    byte[] authenticatorData = authenticator.BuildAuthenticatorData("localhost", includeAttestedCredentialData: true);
    byte[] attestationObject = IntegrationSoftwareAuthenticator.BuildAttestationObject(authenticatorData);
    byte[] clientDataJson = IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, origin);

    var registerCommand = new CompletePasskeyRegistration.Command
    {
      CredentialId = Base64Url.EncodeToString(authenticator.CredentialId),
      ClientDataJson = Base64Url.EncodeToString(clientDataJson),
      AttestationObject = Base64Url.EncodeToString(attestationObject)
    };

    var testApiService = new TestApiService(WebTestServerApplication.HttpClient, ContractSerializationDefaults.Options);
    HttpResponseMessage registerResponse = await testApiService.GetHttpResponseMessage(registerCommand, CancellationToken.None);
    registerResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    string? sessionCookie = setCookieValues!.FirstOrDefault(value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
    sessionCookie.ShouldNotBeNull("Expected the passkey registration to issue an identity-session cookie.");

    using HttpClient isolatedClient = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    isolatedClient.DefaultRequestHeaders.Add("Cookie", sessionCookie.Split(';')[0]);
    // Deliberately NO Authorization header — only the cookie.

    HttpResponseMessage response = await isolatedClient.GetAsync(GetAgentIdentity.Query.RouteTemplate);

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  private async Task<string> RegisterAndIssueToken(IntegrationSoftwareAgentKey key, List<string> scopes)
  {
    OneOf<StartAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> registerStart =
      await WebTestServerApplication.GetResponse<StartAgentKeyRegistration.Response>(new StartAgentKeyRegistration.Command(), CancellationToken.None);
    byte[] registerChallenge = Base64Url.DecodeFromChars(registerStart.AsT0.Challenge);
    byte[] registerSignature = key.Sign(AgentKeyCeremonyType.Registration, registerChallenge);

    var registerCommand = new CompleteAgentKeyRegistration.Command
    {
      PublicKey = Base64Url.EncodeToString(key.SpkiPublicKey),
      Challenge = Base64Url.EncodeToString(registerChallenge),
      Signature = Base64Url.EncodeToString(registerSignature)
    };

    OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> registerResult =
      await WebTestServerApplication.GetResponse<CompleteAgentKeyRegistration.Response>(registerCommand, CancellationToken.None);
    registerResult.IsT0.ShouldBeTrue("Registration setup should succeed.");

    OneOf<StartAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> tokenStart =
      await WebTestServerApplication.GetResponse<StartAgentTokenIssuance.Response>(new StartAgentTokenIssuance.Command(), CancellationToken.None);
    byte[] tokenChallenge = Base64Url.DecodeFromChars(tokenStart.AsT0.Challenge);
    byte[] tokenSignature = key.Sign(AgentKeyCeremonyType.TokenIssuance, tokenChallenge);

    var tokenCommand = new CompleteAgentTokenIssuance.Command
    {
      KeyId = registerResult.AsT0.KeyId,
      Challenge = Base64Url.EncodeToString(tokenChallenge),
      Signature = Base64Url.EncodeToString(tokenSignature),
      Scopes = scopes
    };

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> tokenResult =
      await WebTestServerApplication.GetResponse<CompleteAgentTokenIssuance.Response>(tokenCommand, CancellationToken.None);
    tokenResult.IsT0.ShouldBeTrue("Token issuance setup should succeed.");

    return tokenResult.AsT0.AccessToken;
  }

  private async Task<HttpResponseMessage> GetAgentMeWithBearer(string accessToken)
  {
    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    return await client.GetAsync(GetAgentIdentity.Query.RouteTemplate);
  }

  private static byte[] ReadWebAuthnChallenge(string optionsJson)
  {
    using JsonDocument document = JsonDocument.Parse(optionsJson);
    string challengeBase64Url = document.RootElement.GetProperty("challenge").GetString()!;
    return Base64Url.DecodeFromChars(challengeBase64Url);
  }
}
