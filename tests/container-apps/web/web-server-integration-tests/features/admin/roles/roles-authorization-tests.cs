#region Purpose
// End-to-end proof that admin Roles endpoints require Administrator (task 147-004), not any
// authenticated principal (task 110's identity-session-only posture is retired for Roles).
#endregion

#region Design
// Deliberately real HTTP with isolated HttpClients — never WebTestServerApplication.Send
// (ScopedSender, in-process mediator). That is precisely the reason the pre-110 bug was invisible:
// Roles_Endpoint_Tests.cs (RolesEndpoints_) uses .Send() throughout, which bypasses ASP.NET Core's
// authentication/authorization middleware entirely (no HttpContext, no [EndpointAuthorize] policy
// evaluation) — those tests stayed green whether the generated endpoint was AllowAnonymous() or
// Policies("..."), so they could never have caught this class of bug and still can't. Only a real
// HTTP round-trip through the actual ASP.NET Core pipeline exercises FastEndpoints' Policies(...)
// enforcement.
// The passkey-ceremony cookie mint here duplicates Passkey_Registration_Tests.cs's flow rather than
// calling into it — small deliberate duplication (same rationale as
// IntegrationSoftwareAuthenticator's own Design region: reaching across test files for ~15 lines
// isn't worth the coupling). Each test builds its own isolated HttpClient (never the shared
// WebTestServerApplication.HttpClient) for the SAME per-class-fixture-sharing reason documented in
// Passkey_Registration_Tests.cs's Design region — an anonymous-request test and an
// authenticated-request test must not leak cookies between them via an ambient jar.
// Task 147-004: Member-only passkey session → 403; after IPrincipalRoleStore grants Administrator
// → 200. Agent bearer still 401 on cookie-only admin routes (scheme isolation).
#endregion

namespace RolesAuthorization_;

using System.Buffers.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Roles;
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

  public static async Task Unauthorized_Given_Anonymous_Get()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };

    HttpResponseMessage response = await client.GetAsync("api/Roles");

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Unauthorized_Given_Anonymous_Post()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    const string requestJson = """{"userId":"11111111-1111-1111-1111-111111111111","name":"Auditor","description":"Read-only access."}""";

    HttpResponseMessage response = await client.PostAsync
      ("api/Roles", new StringContent(requestJson, Encoding.UTF8, "application/json"));

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Forbidden_Given_Passkey_Member_Only_Session()
  {
    (string sessionCookie, _) = await MintIdentitySessionCookie();

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    var query = new GetRoles.Query { UserId = Guid.NewGuid() };
    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  public static async Task Forbidden_Create_Given_Passkey_Member_Only_Session()
  {
    (string sessionCookie, _) = await MintIdentitySessionCookie();

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    const string requestJson = """{"userId":"11111111-1111-1111-1111-111111111111","name":"Auditor","description":"Read-only access."}""";

    HttpResponseMessage response = await client.PostAsync(
      "api/Roles",
      new StringContent(requestJson, Encoding.UTF8, "application/json"));

    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  public static async Task Ok_With_Seeded_Roles_Given_Administrator_Via_Role_Store()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie();

    // Scope required: under postgres IPrincipalRoleStore is scoped (EfPrincipalRoleStore).
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    var query = new GetRoles.Query { UserId = Guid.NewGuid() };
    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    json.ShouldContain(nameof(RoleIds.Administrator));
  }

  public static async Task Unauthorized_Given_Agent_Bearer_Token_No_Cookie()
  {
    string accessToken = await RegisterAndIssueAgentBearerToken();

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    // Deliberately NO Cookie header — only the bearer token.

    HttpResponseMessage response = await client.GetAsync("api/Roles");

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  private static async Task<string> RegisterAndIssueAgentBearerToken()
  {
    var key = new IntegrationSoftwareAgentKey();

    OneOf<StartAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> registerStart =
      await Web.GetResponse<StartAgentKeyRegistration.Response>(new StartAgentKeyRegistration.Command(), CancellationToken.None);
    byte[] registerChallenge = Base64Url.DecodeFromChars(registerStart.AsT0.Challenge);
    byte[] registerSignature = key.Sign(AgentKeyCeremonyType.Registration, registerChallenge);

    var registerCommand = new CompleteAgentKeyRegistration.Command
    {
      PublicKey = Base64Url.EncodeToString(key.SpkiPublicKey),
      Challenge = Base64Url.EncodeToString(registerChallenge),
      Signature = Base64Url.EncodeToString(registerSignature)
    };

    OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> registerResult =
      await Web.GetResponse<CompleteAgentKeyRegistration.Response>(registerCommand, CancellationToken.None);
    registerResult.IsT0.ShouldBeTrue("Registration setup should succeed.");

    OneOf<StartAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> tokenStart =
      await Web.GetResponse<StartAgentTokenIssuance.Response>(new StartAgentTokenIssuance.Command(), CancellationToken.None);
    byte[] tokenChallenge = Base64Url.DecodeFromChars(tokenStart.AsT0.Challenge);
    byte[] tokenSignature = key.Sign(AgentKeyCeremonyType.TokenIssuance, tokenChallenge);

    var tokenCommand = new CompleteAgentTokenIssuance.Command
    {
      KeyId = registerResult.AsT0.KeyId,
      Challenge = Base64Url.EncodeToString(tokenChallenge),
      Signature = Base64Url.EncodeToString(tokenSignature),
      Scopes = [AgentScopes.IdentityRead]
    };

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> tokenResult =
      await Web.GetResponse<CompleteAgentTokenIssuance.Response>(tokenCommand, CancellationToken.None);
    tokenResult.IsT0.ShouldBeTrue("Token issuance setup should succeed.");

    return tokenResult.AsT0.AccessToken;
  }

  private static async Task<(string Cookie, PrincipalId PrincipalId)> MintIdentitySessionCookie()
  {
    var authenticator = new IntegrationSoftwareAuthenticator();
    var testApiService = new TestApiService(Web.HttpClient, ContractSerializationDefaults.Options);

    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);
    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = Web.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

    byte[] authenticatorData = authenticator.BuildAuthenticatorData("localhost", includeAttestedCredentialData: true);
    byte[] attestationObject = IntegrationSoftwareAuthenticator.BuildAttestationObject(authenticatorData);
    byte[] clientDataJson = IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, origin);

    var registerCommand = new CompletePasskeyRegistration.Command
    {
      CredentialId = Base64Url.EncodeToString(authenticator.CredentialId),
      ClientDataJson = Base64Url.EncodeToString(clientDataJson),
      AttestationObject = Base64Url.EncodeToString(attestationObject)
    };

    HttpResponseMessage registerResponse = await testApiService.GetHttpResponseMessage(registerCommand, CancellationToken.None);
    registerResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    string? sessionCookie = setCookieValues!.FirstOrDefault
      (value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
    sessionCookie.ShouldNotBeNull("Expected the passkey registration to issue an identity-session cookie.");

    string cookieHeader = sessionCookie.Split(';')[0];

    using HttpClient sessionClient = new() { BaseAddress = Web.HttpClient.BaseAddress };
    sessionClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);
    HttpResponseMessage sessionResponse = await sessionClient.GetAsync(GetCurrentSession.Query.RouteTemplate);
    sessionResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    string sessionJson = await sessionResponse.Content.ReadAsStringAsync();
    GetCurrentSession.Response session =
      JsonSerializer.Deserialize<GetCurrentSession.Response>(sessionJson, ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("GetCurrentSession deserialized to null.");
    session.IsAuthenticated.ShouldBeTrue();
    session.PrincipalId.ShouldNotBeNull();
    session.RoleIds.ShouldContain(RoleIds.Member);

    return (cookieHeader, session.PrincipalId.Value);
  }

  private static byte[] ReadChallenge(string optionsJson)
  {
    using JsonDocument document = JsonDocument.Parse(optionsJson);
    string challengeBase64Url = document.RootElement.GetProperty("challenge").GetString()!;
    return Base64Url.DecodeFromChars(challengeBase64Url);
  }

}
