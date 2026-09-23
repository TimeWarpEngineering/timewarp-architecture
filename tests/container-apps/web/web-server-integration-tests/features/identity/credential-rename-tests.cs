#region Purpose
// End-to-end proof of task 248-001's RenameCredential endpoint over real HTTP: the caller can rename
// their OWN credential (cookie and bearer), validation rejects an oversize nickname, and another
// principal's credential is a 404 indistinguishable from an unknown id.
#endregion

#region Design
// Real HTTP with isolated HttpClients — same rationale as Credential_Revoke_Tests.cs (only a real
// round-trip exercises [EndpointAuthorize(Policy=credential.manage.self)] on the dual scheme). The
// handler-level happy/404/validator matrix lives in the co-located rename-credential-tests.cs
// runfile; this file pins the HTTP seam (route, auth, status mapping, and that GetCredentials reflects
// the new nickname while Label stays the provider name).
#endregion

namespace CredentialRename_;

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

  public static async Task Unauthorized_Given_Anonymous_Rename()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    var command = new RenameCredential.Command { UserId = Guid.NewGuid(), CredentialId = Guid.NewGuid(), Nickname = "x" };

    HttpResponseMessage response = await testApiService.GetHttpResponseMessage(command, CancellationToken.None);

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Ok_And_Reflected_Given_Own_Credential_Via_Cookie()
  {
    (PrincipalId _, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);

    GetCredentials.Response before = await ListAsync(client);
    GetCredentials.CredentialSummary target = before.Credentials.Single();
    target.Nickname.ShouldBeNull();

    var command = new RenameCredential.Command { UserId = Guid.NewGuid(), CredentialId = target.Id.Value, Nickname = "  Work laptop " };
    HttpResponseMessage response = await testApiService.GetHttpResponseMessage(command, CancellationToken.None);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    GetCredentials.CredentialSummary after = (await ListAsync(client)).Credentials.Single();
    after.Nickname.ShouldBe("Work laptop");
    after.Label.ShouldBe(target.Label);
    after.Fingerprint.ShouldBe(target.Fingerprint);
  }

  public static async Task Ok_Given_Own_Credential_Via_Agent_Bearer_Token()
  {
    var key = new IntegrationSoftwareAgentKey();
    (PrincipalId _, string _, string accessToken) =
      await CredentialCeremonyHelpers.RegisterAgentKeyAndIssueTokenAsync(Web, key, [AgentScopes.CredentialManage]);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);

    GetCredentials.CredentialSummary target = (await ListAsync(client)).Credentials.Single();
    var command = new RenameCredential.Command { UserId = Guid.NewGuid(), CredentialId = target.Id.Value, Nickname = "ci-runner" };

    HttpResponseMessage response = await testApiService.GetHttpResponseMessage(command, CancellationToken.None);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    (await ListAsync(client)).Credentials.Single().Nickname.ShouldBe("ci-runner");
  }

  public static async Task ValidationError_Given_Oversize_Nickname()
  {
    (PrincipalId _, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    GetCredentials.CredentialSummary target = (await ListAsync(client)).Credentials.Single();

    var command = new RenameCredential.Command
    {
      UserId = Guid.NewGuid(),
      CredentialId = target.Id.Value,
      Nickname = new string('x', Credential.MaxNicknameLength + 1)
    };

    HttpResponseMessage response = await testApiService.GetHttpResponseMessage(command, CancellationToken.None);

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    (await ListAsync(client)).Credentials.Single().Nickname.ShouldBeNull();
  }

  public static async Task NotFound_Given_Another_Principals_Credential_And_Unknown_Id_Alike()
  {
    (PrincipalId _, string victimCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    using HttpClient victim = new() { BaseAddress = Web.HttpClient.BaseAddress };
    victim.DefaultRequestHeaders.Add("Cookie", victimCookie);
    GetCredentials.CredentialSummary victimCredential = (await ListAsync(victim)).Credentials.Single();

    (PrincipalId _, string attackerCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    using HttpClient attacker = new() { BaseAddress = Web.HttpClient.BaseAddress };
    attacker.DefaultRequestHeaders.Add("Cookie", attackerCookie);
    var testApiService = new TestApiService(attacker, ContractSerializationDefaults.Options, bearerToken: null);

    HttpResponseMessage crossPrincipal = await testApiService.GetHttpResponseMessage(
      new RenameCredential.Command { UserId = Guid.NewGuid(), CredentialId = victimCredential.Id.Value, Nickname = "hijacked" },
      CancellationToken.None);
    HttpResponseMessage unknown = await testApiService.GetHttpResponseMessage(
      new RenameCredential.Command { UserId = Guid.NewGuid(), CredentialId = Guid.NewGuid(), Nickname = "ghost" },
      CancellationToken.None);

    // Same status AND same body shape — no existence oracle (RevokeCredential's rule, reused).
    crossPrincipal.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    unknown.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    (await crossPrincipal.Content.ReadAsStringAsync()).ShouldBe(await unknown.Content.ReadAsStringAsync());
    (await ListAsync(victim)).Credentials.Single().Nickname.ShouldBeNull();
  }

  private static async Task<GetCredentials.Response> ListAsync(HttpClient client)
  {
    var query = new GetCredentials.Query { UserId = Guid.NewGuid(), IncludeRevoked = true };
    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    GetCredentials.Response? parsed =
      JsonSerializer.Deserialize<GetCredentials.Response>(await response.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    parsed.ShouldNotBeNull();
    return parsed;
  }
}
