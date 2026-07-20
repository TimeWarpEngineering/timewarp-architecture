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
// The no-secret-material assertion checks the RAW response body string, not just the deserialized
// CredentialSummary shape — the contract type not HAVING a Handle/PublicMaterial property already
// makes leaking them structurally impossible from THIS handler, but the wire-level check is what
// actually pins the promise end-to-end (matches the plan's explicit "pin with
// json.ShouldNotContain" instruction) and would catch a future contract change that added the fields
// back.
#endregion

namespace CredentialList_;

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

  public async Task Unauthorized_Given_Anonymous_Request()
  {
    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    var query = new GetCredentials.Query { UserId = Guid.NewGuid() };

    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public async Task Ok_With_Own_Credential_Given_Cookie_Session()
  {
    (PrincipalId _, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(WebTestServerApplication);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var query = new GetCredentials.Query { UserId = Guid.NewGuid() };

    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    json.ShouldContain("\"type\":\"Passkey\"");
    json.ShouldContain("\"isActive\":true");
  }

  public async Task Ok_With_Own_Credential_Given_Agent_Bearer_Token()
  {
    var key = new IntegrationSoftwareAgentKey();
    (PrincipalId _, string _, string accessToken) =
      await CredentialCeremonyHelpers.RegisterAgentKeyAndIssueTokenAsync(WebTestServerApplication, key, [AgentScopes.CredentialManage]);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    var query = new GetCredentials.Query { UserId = Guid.NewGuid() };

    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    json.ShouldContain("\"type\":\"AgentKey\"");
    json.ShouldContain("\"isActive\":true");
  }

  public async Task Forbidden_Given_IdentityReadOnly_Bearer_Token()
  {
    var key = new IntegrationSoftwareAgentKey();
    (PrincipalId _, string _, string accessToken) =
      await CredentialCeremonyHelpers.RegisterAgentKeyAndIssueTokenAsync(WebTestServerApplication, key, [AgentScopes.IdentityRead]);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    var query = new GetCredentials.Query { UserId = Guid.NewGuid() };

    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    // Load-bearing least-privilege assertion (CredentialManagementDefaults' Design region): an
    // identity:read-only token is a validly authenticated agent principal, but the policy's
    // RequireAssertion specifically requires the credential:manage scope for bearer callers.
    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  public async Task Excludes_Revoked_By_Default_And_Includes_When_Asked()
  {
    (PrincipalId _, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(WebTestServerApplication);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);

    // Add a second credential, then revoke it, so this principal has one active + one revoked.
    (string credentialId, string clientDataJson, string attestationObject) =
      await CredentialCeremonyHelpers.BuildPasskeyAttestationAsync(WebTestServerApplication);
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

  public async Task Never_Serializes_Handle_Or_PublicMaterial()
  {
    (PrincipalId _, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(WebTestServerApplication);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var query = new GetCredentials.Query { UserId = Guid.NewGuid() };

    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    json.ToLowerInvariant().ShouldNotContain("handle");
    json.ToLowerInvariant().ShouldNotContain("publicmaterial");
  }
}
