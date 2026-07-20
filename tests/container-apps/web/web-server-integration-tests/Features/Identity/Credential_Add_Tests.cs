#region Purpose
// End-to-end proof of task 104-005's AddPasskey/AddAgentKey endpoints: both attach to the CALLER's
// EXISTING principal (never mint a new one), and agent key rotation (add new, revoke old) leaves
// exactly one active key.
#endregion

#region Design
// Deliberately real HTTP with isolated HttpClients — same rationale as Credential_List_Tests.cs.
// Ceremony setup goes through CredentialCeremonyHelpers; a FRESH IntegrationSoftwareAuthenticator/
// IntegrationSoftwareAgentKey per credential is what makes "add a SECOND credential to the same
// principal" possible without a handle/KeyId collision against the first (see those fixtures' own
// Design regions).
#endregion

namespace CredentialAdd_;

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

  public async Task Unauthorized_Given_Anonymous_AddPasskey()
  {
    (string credentialId, string clientDataJson, string attestationObject) =
      await CredentialCeremonyHelpers.BuildPasskeyAttestationAsync(WebTestServerApplication);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    var command = new AddPasskey.Command
    {
      UserId = Guid.NewGuid(),
      CredentialId = credentialId,
      ClientDataJson = clientDataJson,
      AttestationObject = attestationObject
    };

    HttpResponseMessage response = await testApiService.GetHttpResponseMessage(command, CancellationToken.None);

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public async Task Ok_With_Two_Active_Credentials_Given_AddPasskey_On_Cookie_Principal()
  {
    (PrincipalId principalId, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(WebTestServerApplication);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);

    (string credentialId, string clientDataJson, string attestationObject) =
      await CredentialCeremonyHelpers.BuildPasskeyAttestationAsync(WebTestServerApplication);
    var addCommand = new AddPasskey.Command
    {
      UserId = Guid.NewGuid(),
      CredentialId = credentialId,
      ClientDataJson = clientDataJson,
      AttestationObject = attestationObject,
      Label = "second-device"
    };

    HttpResponseMessage addResponse = await testApiService.GetHttpResponseMessage(addCommand, CancellationToken.None);
    addResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    AddPasskey.Response? addResult =
      JsonSerializer.Deserialize<AddPasskey.Response>(await addResponse.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    addResult.ShouldNotBeNull();
    addResult.CredentialId.IsEmpty.ShouldBeFalse();

    var listQuery = new GetCredentials.Query { UserId = Guid.NewGuid() };
    HttpResponseMessage listResponse = await client.GetAsync(listQuery.GetRouteWithQueryString());
    GetCredentials.Response? list =
      JsonSerializer.Deserialize<GetCredentials.Response>(await listResponse.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    list.ShouldNotBeNull();
    list.Credentials.Count.ShouldBe(2);
    list.Credentials.ShouldAllBe(c => c.IsActive);
    list.Credentials.ShouldContain(c => c.Label == "second-device");
  }

  public async Task Conflict_Given_Same_Passkey_Handle_Registered_Twice()
  {
    (PrincipalId _, string sessionCookie) =
      await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(WebTestServerApplication);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);

    // Two INDEPENDENT ceremonies (fresh challenge each) but the SAME authenticator instance, so the
    // underlying credential HANDLE collides — a literal replay of one already-submitted command
    // would 400 on the one-time challenge before ever reaching the duplicate-handle check, so this
    // is the only way to exercise that specific 409 path.
    var authenticator = new IntegrationSoftwareAuthenticator();

    (string firstCredentialId, string firstClientDataJson, string firstAttestationObject) =
      await CredentialCeremonyHelpers.BuildPasskeyAttestationAsync(WebTestServerApplication, authenticator);
    var firstCommand = new AddPasskey.Command
    {
      UserId = Guid.NewGuid(),
      CredentialId = firstCredentialId,
      ClientDataJson = firstClientDataJson,
      AttestationObject = firstAttestationObject
    };
    HttpResponseMessage firstResponse = await testApiService.GetHttpResponseMessage(firstCommand, CancellationToken.None);
    firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

    (string secondCredentialId, string secondClientDataJson, string secondAttestationObject) =
      await CredentialCeremonyHelpers.BuildPasskeyAttestationAsync(WebTestServerApplication, authenticator);
    var secondCommand = new AddPasskey.Command
    {
      UserId = Guid.NewGuid(),
      CredentialId = secondCredentialId,
      ClientDataJson = secondClientDataJson,
      AttestationObject = secondAttestationObject
    };
    HttpResponseMessage secondResponse = await testApiService.GetHttpResponseMessage(secondCommand, CancellationToken.None);
    secondResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
  }

  public async Task Ok_With_Two_Active_Credentials_Given_AddAgentKey_On_Bearer_Principal()
  {
    var firstKey = new IntegrationSoftwareAgentKey();
    (PrincipalId principalId, string _, string accessToken) =
      await CredentialCeremonyHelpers.RegisterAgentKeyAndIssueTokenAsync(WebTestServerApplication, firstKey, [AgentScopes.CredentialManage]);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);

    var secondKey = new IntegrationSoftwareAgentKey();
    (string publicKey, string challenge, string signature) =
      await CredentialCeremonyHelpers.BuildAgentKeyRegistrationProofAsync(WebTestServerApplication, secondKey);
    var addCommand = new AddAgentKey.Command
    {
      UserId = Guid.NewGuid(),
      PublicKey = publicKey,
      Challenge = challenge,
      Signature = signature,
      Label = "rotated-key"
    };

    HttpResponseMessage addResponse = await testApiService.GetHttpResponseMessage(addCommand, CancellationToken.None);
    addResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    AddAgentKey.Response? addResult =
      JsonSerializer.Deserialize<AddAgentKey.Response>(await addResponse.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    addResult.ShouldNotBeNull();

    var listQuery = new GetCredentials.Query { UserId = Guid.NewGuid() };
    HttpResponseMessage listResponse = await client.GetAsync(listQuery.GetRouteWithQueryString());
    GetCredentials.Response? list =
      JsonSerializer.Deserialize<GetCredentials.Response>(await listResponse.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    list.ShouldNotBeNull();
    list.Credentials.Count.ShouldBe(2);
    list.Credentials.ShouldAllBe(c => c.IsActive);
  }

  public async Task Ok_With_One_Active_Key_Given_Rotation_Adds_New_Then_Revokes_Old()
  {
    var originalKey = new IntegrationSoftwareAgentKey();
    (PrincipalId _, string _, string accessToken) =
      await CredentialCeremonyHelpers.RegisterAgentKeyAndIssueTokenAsync(WebTestServerApplication, originalKey, [AgentScopes.CredentialManage]);

    using HttpClient client = new() { BaseAddress = WebTestServerApplication.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);

    // 1. Discover the original key's CredentialId via the list endpoint (rotation, like a real
    //    caller, does not already know it — only the KeyId from registration).
    var listBeforeQuery = new GetCredentials.Query { UserId = Guid.NewGuid() };
    HttpResponseMessage listBeforeResponse = await client.GetAsync(listBeforeQuery.GetRouteWithQueryString());
    GetCredentials.Response? listBefore =
      JsonSerializer.Deserialize<GetCredentials.Response>(await listBeforeResponse.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    listBefore.ShouldNotBeNull();
    listBefore.Credentials.Count.ShouldBe(1);
    CredentialId originalCredentialId = listBefore.Credentials[0].Id;

    // 2. Add the new key.
    var newKey = new IntegrationSoftwareAgentKey();
    (string publicKey, string challenge, string signature) =
      await CredentialCeremonyHelpers.BuildAgentKeyRegistrationProofAsync(WebTestServerApplication, newKey);
    var addCommand = new AddAgentKey.Command
    {
      UserId = Guid.NewGuid(),
      PublicKey = publicKey,
      Challenge = challenge,
      Signature = signature
    };
    HttpResponseMessage addResponse = await testApiService.GetHttpResponseMessage(addCommand, CancellationToken.None);
    addResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

    // 3. Revoke the original key — the bearer token that authorizes this call was issued to the
    //    ORIGINAL key's proof-of-possession ceremony, but credential-management authorizes the
    //    PRINCIPAL, not the specific key that was presented, so revoking that same principal's own
    //    now-superseded key is expected to succeed.
    var revokeCommand = new RevokeCredential.Command { UserId = Guid.NewGuid(), CredentialId = originalCredentialId.Value };
    HttpResponseMessage revokeResponse = await testApiService.GetHttpResponseMessage(revokeCommand, CancellationToken.None);
    revokeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

    var listAfterQuery = new GetCredentials.Query { UserId = Guid.NewGuid() };
    HttpResponseMessage listAfterResponse = await client.GetAsync(listAfterQuery.GetRouteWithQueryString());
    GetCredentials.Response? listAfter =
      JsonSerializer.Deserialize<GetCredentials.Response>(await listAfterResponse.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    listAfter.ShouldNotBeNull();
    listAfter.Credentials.Count.ShouldBe(1);
    listAfter.Credentials[0].Id.ShouldNotBe(originalCredentialId);
  }
}
