#region Purpose
// End-to-end proof that ListPrincipals / SetPrincipalRoles require Administrator (task 147-004).
#endregion

#region Design
// Mirrors roles-authorization-tests: real HTTP, isolated clients, passkey cookie mint, grant via
// IPrincipalRoleStore, then assert 403 → 200. Agent bearer remains 401 (cookie-scheme policy).
#endregion

namespace PrincipalsAuthorization_;

using System.Buffers.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Principals;
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

  public static async Task Unauthorized_Given_Anonymous_List()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    HttpResponseMessage response = await client.GetAsync("api/admin/principals");
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Forbidden_Given_Passkey_Member_Only_Session()
  {
    (string sessionCookie, _) = await MintIdentitySessionCookie();

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    var query = new ListPrincipals.Query { UserId = Guid.NewGuid() };
    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  public static async Task Ok_List_Given_Administrator_Via_Role_Store()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie();

    // Scope required: under postgres IPrincipalRoleStore is scoped (EfPrincipalRoleStore).
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    var query = new ListPrincipals.Query { UserId = Guid.NewGuid() };
    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    json.ShouldContain(principalId.ToString());
  }

  public static async Task Ok_SetRoles_Given_Administrator_Via_Role_Store()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie();

    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    string body = JsonSerializer.Serialize(
      new
      {
        userId = Guid.NewGuid(),
        roleIds = new[] { RoleIds.Member, RoleIds.Developer }
      },
      ContractSerializationDefaults.Options);

    HttpResponseMessage response = await client.PutAsync(
      $"api/admin/principals/{principalId}/roles",
      new StringContent(body, Encoding.UTF8, "application/json"));

    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    IReadOnlyList<Guid> stored = await roleStore.GetRoleIdsAsync(principalId);
    stored.ShouldContain(RoleIds.Developer);
    stored.ShouldContain(RoleIds.Member);
  }

  public static async Task Unauthorized_Given_Agent_Bearer_Token_No_Cookie()
  {
    string accessToken = await RegisterAndIssueAgentBearerToken();

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    HttpResponseMessage response = await client.GetAsync("api/admin/principals");
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

    return (cookieHeader, session.PrincipalId.Value);
  }

  private static byte[] ReadChallenge(string optionsJson)
  {
    using JsonDocument document = JsonDocument.Parse(optionsJson);
    string challengeBase64Url = document.RootElement.GetProperty("challenge").GetString()!;
    return Base64Url.DecodeFromChars(challengeBase64Url);
  }
}
