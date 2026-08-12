#region Purpose
// End-to-end proof of task 104-005's GetCredentials endpoint: both auth schemes can list their OWN
// credentials, revoked entries are hidden unless asked for, an anonymous caller is rejected, and —
// load-bearing, security — the wire response never carries Handle/PublicMaterial.
#endregion

#region Design
// Deliberately real HTTP with isolated HttpClients — same rationale as Roles_Authorization_Tests.cs's
// Design region (only a real round-trip through the ASP.NET Core pipeline exercises
// [EndpointAuthorize(Policy="credential-management")]'s either-scheme RequireAssertion). Ceremony
// setup goes through CredentialCeremonyHelpers (see that file's Design region for why this task's
// setup is shared rather than duplicated per file).
// The no-secret-material assertion is two layers (round-1 review M4): a STRUCTURAL check
// (reflection over CredentialSummary's own property set) is the real guarantee and cannot false-fail
// on Label content, plus the wire-level json.ShouldNotContain as belt-and-suspenders (matches the
// plan's explicit "pin with json.ShouldNotContain" instruction) — it would catch a future contract
// change that added the fields back even if a reviewer forgot to update the reflection check. The
// wire check alone was fragile: a Label containing the substring "handle" would false-fail it, which
// is exactly why the structural check is the one this test actually trusts.
#endregion

namespace CredentialList_;

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
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

  public static async Task Unauthorized_Given_Anonymous_Request()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    var query = new GetCredentials.Query { UserId = Guid.NewGuid() };

    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Ok_With_Own_Credential_Given_Cookie_Session()
  {
    (PrincipalId _, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var query = new GetCredentials.Query { UserId = Guid.NewGuid() };

    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    json.ShouldContain("\"type\":\"Passkey\"");
    json.ShouldContain("\"isActive\":true");
  }

  public static async Task Ok_With_Own_Credential_Given_Agent_Bearer_Token()
  {
    var key = new IntegrationSoftwareAgentKey();
    (PrincipalId _, string _, string accessToken) =
      await CredentialCeremonyHelpers.RegisterAgentKeyAndIssueTokenAsync(Web, key, [AgentScopes.CredentialManage]);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    var query = new GetCredentials.Query { UserId = Guid.NewGuid() };

    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    json.ShouldContain("\"type\":\"AgentKey\"");
    json.ShouldContain("\"isActive\":true");
  }

  public static async Task Forbidden_Given_IdentityReadOnly_Bearer_Token()
  {
    var key = new IntegrationSoftwareAgentKey();
    (PrincipalId _, string _, string accessToken) =
      await CredentialCeremonyHelpers.RegisterAgentKeyAndIssueTokenAsync(Web, key, [AgentScopes.IdentityRead]);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    var query = new GetCredentials.Query { UserId = Guid.NewGuid() };

    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    // Load-bearing least-privilege assertion (CredentialManagementDefaults' Design region): an
    // identity:read-only token is a validly authenticated agent principal, but the policy's
    // RequireAssertion specifically requires the credential:manage scope for bearer callers.
    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  public static async Task Excludes_Revoked_By_Default_And_Includes_When_Asked()
  {
    (PrincipalId _, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);

    // Add a second credential, then revoke it, so this principal has one active + one revoked.
    (string credentialId, string clientDataJson, string attestationObject) =
      await CredentialCeremonyHelpers.BuildPasskeyAttestationAsync(Web);
    var addCommand = new AddPasskey.Command
    {
      UserId = Guid.NewGuid(),
      CredentialId = credentialId,
      ClientDataJson = clientDataJson,
      AttestationObject = attestationObject
    };
    HttpResponseMessage addResponse = await testApiService.GetHttpResponseMessage(addCommand, CancellationToken.None);
    addResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    AddPasskey.Response? addResult =
      JsonSerializer.Deserialize<AddPasskey.Response>(await addResponse.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    addResult.ShouldNotBeNull();

    var revokeCommand = new RevokeCredential.Command { UserId = Guid.NewGuid(), CredentialId = addResult.CredentialId.Value };
    HttpResponseMessage revokeResponse = await testApiService.GetHttpResponseMessage(revokeCommand, CancellationToken.None);
    revokeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

    var activeOnlyQuery = new GetCredentials.Query { UserId = Guid.NewGuid(), IncludeRevoked = false };
    HttpResponseMessage activeOnlyResponse = await client.GetAsync(activeOnlyQuery.GetRouteWithQueryString());
    GetCredentials.Response? activeOnly = JsonSerializer.Deserialize<GetCredentials.Response>
      (await activeOnlyResponse.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    activeOnly.ShouldNotBeNull();
    activeOnly.Credentials.Count.ShouldBe(1);
    activeOnly.Credentials[0].IsActive.ShouldBeTrue();

    var allQuery = new GetCredentials.Query { UserId = Guid.NewGuid(), IncludeRevoked = true };
    HttpResponseMessage allResponse = await client.GetAsync(allQuery.GetRouteWithQueryString());
    GetCredentials.Response? all = JsonSerializer.Deserialize<GetCredentials.Response>
      (await allResponse.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    all.ShouldNotBeNull();
    all.Credentials.Count.ShouldBe(2);
    all.Credentials.Count(c => !c.IsActive).ShouldBe(1);
  }

  public static async Task Never_Serializes_Handle_Or_PublicMaterial()
  {
    // Structural check FIRST (round-1 review M4) — the real guarantee, and the only one that cannot
    // false-fail on Label content: CredentialSummary itself has no Handle/PublicMaterial member, so
    // no future handler change could serialize either field even by accident.
    string[] propertyNames = typeof(GetCredentials.CredentialSummary).GetProperties().Select(p => p.Name).ToArray();
    propertyNames.ShouldNotContain(nameof(Credential.Handle));
    propertyNames.ShouldNotContain(nameof(Credential.PublicMaterial));

    (PrincipalId _, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var query = new GetCredentials.Query { UserId = Guid.NewGuid() };

    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    // Wire-level check SECOND, belt-and-suspenders — catches a future contract change that added
    // the fields back even if a reviewer forgot to update the structural check above.
    string json = await response.Content.ReadAsStringAsync();
    json.ToLowerInvariant().ShouldNotContain("handle");
    json.ToLowerInvariant().ShouldNotContain("publicmaterial");
  }

}
