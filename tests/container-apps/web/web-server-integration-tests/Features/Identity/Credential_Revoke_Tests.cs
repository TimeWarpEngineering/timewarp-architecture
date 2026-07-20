#region Purpose
// End-to-end proof of task 104-005's RevokeCredential endpoint: both auth schemes can revoke their
// OWN credentials, and every rejection branch documented on RevokeCredential.Handler's Design
// region — IDOR (404, never 403), last-active-credential guard (409), already-revoked (409),
// unauthenticated (401), and insufficient scope (403) — is real over HTTP, not just annotation.
#endregion

#region Design
// Deliberately real HTTP with isolated HttpClients — same rationale as the sibling
// Credential_List_Tests.cs/Credential_Add_Tests.cs. The retry-loop's OWN concurrency behavior
// (bounded catch-reGet-retry, contention exhaustion) is covered separately and deterministically at
// the handler seam by RevokeCredential_ConcurrencyRetry_Tests.cs — a real HTTP race is
// non-deterministic, so this file sticks to the single-actor rejection/success paths a real client
// can trigger deterministically.
#endregion

namespace CredentialRevoke_;

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

  public async Task Unauthorized_Given_Anonymous_Revoke()
  {
    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    var command = new RevokeCredential.Command { UserId = Guid.NewGuid(), CredentialId = Guid.NewGuid() };

    HttpResponseMessage response = await testApiService.GetHttpResponseMessage(command, CancellationToken.None);

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public async Task Forbidden_Given_IdentityReadOnly_Bearer_Token()
  {
    var key = new IntegrationSoftwareAgentKey();
    (PrincipalId _, string _, string accessToken) =
      await CredentialCeremonyHelpers.RegisterAgentKeyAndIssueTokenAsync(WebTestServerApplication, key, [AgentScopes.IdentityRead]);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    var command = new RevokeCredential.Command { UserId = Guid.NewGuid(), CredentialId = Guid.NewGuid() };

    HttpResponseMessage response = await testApiService.GetHttpResponseMessage(command, CancellationToken.None);

    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  public async Task Ok_And_Reflected_Given_Own_Credential_Via_Cookie()
  {
    (PrincipalId _, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(WebTestServerApplication);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);

    // A cookie principal starts with exactly one credential, and the last-active guard forbids
    // revoking it — add a second so the revoke under test is legal.
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

    var listQuery = new GetCredentials.Query { UserId = Guid.NewGuid(), IncludeRevoked = true };
    HttpResponseMessage listResponse = await client.GetAsync(listQuery.GetRouteWithQueryString());
    GetCredentials.Response? list =
      JsonSerializer.Deserialize<GetCredentials.Response>(await listResponse.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    list.ShouldNotBeNull();
    GetCredentials.CredentialSummary revoked = list.Credentials.Single(c => c.Id == addResult.CredentialId);
    revoked.IsActive.ShouldBeFalse();
    revoked.RevokedAt.ShouldNotBeNull();
  }

  public async Task Ok_Given_Own_Credential_Via_Agent_Bearer_Token()
  {
    var firstKey = new IntegrationSoftwareAgentKey();
    (PrincipalId _, string _, string accessToken) =
      await CredentialCeremonyHelpers.RegisterAgentKeyAndIssueTokenAsync(WebTestServerApplication, firstKey, [AgentScopes.CredentialManage]);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);

    var secondKey = new IntegrationSoftwareAgentKey();
    (string publicKey, string challenge, string signature) =
      await CredentialCeremonyHelpers.BuildAgentKeyRegistrationProofAsync(WebTestServerApplication, secondKey);
    var addCommand = new AddAgentKey.Command { UserId = Guid.NewGuid(), PublicKey = publicKey, Challenge = challenge, Signature = signature };
    HttpResponseMessage addResponse = await testApiService.GetHttpResponseMessage(addCommand, CancellationToken.None);
    addResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    AddAgentKey.Response? addResult =
      JsonSerializer.Deserialize<AddAgentKey.Response>(await addResponse.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    addResult.ShouldNotBeNull();

    var revokeCommand = new RevokeCredential.Command { UserId = Guid.NewGuid(), CredentialId = addResult.CredentialId.Value };
    HttpResponseMessage revokeResponse = await testApiService.GetHttpResponseMessage(revokeCommand, CancellationToken.None);

    revokeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
  }

  public async Task NotFound_Given_Another_Principals_Credential()
  {
    // Load-bearing IDOR assertion: caller A must get the SAME 404 whether the id is unknown or
    // belongs to a DIFFERENT principal — never 403 (see RevokeCredential.Handler's Design region).
    (PrincipalId _, string ownerCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(WebTestServerApplication);
    using HttpClient ownerClient = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    ownerClient.DefaultRequestHeaders.Add("Cookie", ownerCookie);
    var ownerListQuery = new GetCredentials.Query { UserId = Guid.NewGuid() };
    HttpResponseMessage ownerListResponse = await ownerClient.GetAsync(ownerListQuery.GetRouteWithQueryString());
    GetCredentials.Response? ownerList =
      JsonSerializer.Deserialize<GetCredentials.Response>(await ownerListResponse.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    ownerList.ShouldNotBeNull();
    CredentialId ownersCredentialId = ownerList.Credentials.Single().Id;

    (PrincipalId _, string attackerCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(WebTestServerApplication);
    using HttpClient attackerClient = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    attackerClient.DefaultRequestHeaders.Add("Cookie", attackerCookie);
    var attackerApiService = new TestApiService(attackerClient, ContractSerializationDefaults.Options, bearerToken: null);

    var revokeCommand = new RevokeCredential.Command { UserId = Guid.NewGuid(), CredentialId = ownersCredentialId.Value };
    HttpResponseMessage revokeResponse = await attackerApiService.GetHttpResponseMessage(revokeCommand, CancellationToken.None);

    revokeResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
  }

  public async Task NotFound_Given_Unknown_CredentialId()
  {
    (PrincipalId _, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(WebTestServerApplication);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    var command = new RevokeCredential.Command { UserId = Guid.NewGuid(), CredentialId = Guid.NewGuid() };

    HttpResponseMessage response = await testApiService.GetHttpResponseMessage(command, CancellationToken.None);

    // Same status as NotFound_Given_Another_Principals_Credential — no existence oracle.
    response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
  }

  public async Task Conflict_Given_Last_Active_Credential()
  {
    (PrincipalId _, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(WebTestServerApplication);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);

    var listQuery = new GetCredentials.Query { UserId = Guid.NewGuid() };
    HttpResponseMessage listResponse = await client.GetAsync(listQuery.GetRouteWithQueryString());
    GetCredentials.Response? list =
      JsonSerializer.Deserialize<GetCredentials.Response>(await listResponse.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    list.ShouldNotBeNull();
    CredentialId onlyCredentialId = list.Credentials.Single().Id;

    var revokeCommand = new RevokeCredential.Command { UserId = Guid.NewGuid(), CredentialId = onlyCredentialId.Value };
    HttpResponseMessage response = await testApiService.GetHttpResponseMessage(revokeCommand, CancellationToken.None);

    response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
  }

  public async Task Conflict_Given_Already_Revoked_Credential()
  {
    (PrincipalId _, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(WebTestServerApplication);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);

    // Add a second credential so the FIRST revoke of it succeeds (not blocked by the last-credential
    // guard), then attempt to revoke the SAME one again.
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
    AddPasskey.Response? addResult =
      JsonSerializer.Deserialize<AddPasskey.Response>(await addResponse.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    addResult.ShouldNotBeNull();

    var revokeCommand = new RevokeCredential.Command { UserId = Guid.NewGuid(), CredentialId = addResult.CredentialId.Value };
    HttpResponseMessage firstRevokeResponse = await testApiService.GetHttpResponseMessage(revokeCommand, CancellationToken.None);
    firstRevokeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

    HttpResponseMessage secondRevokeResponse = await testApiService.GetHttpResponseMessage(revokeCommand, CancellationToken.None);

    secondRevokeResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
  }
}
