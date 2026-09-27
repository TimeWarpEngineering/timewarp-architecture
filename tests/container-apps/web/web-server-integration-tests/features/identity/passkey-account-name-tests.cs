#region Purpose
// Task 253 end-to-end: the WebAuthn user name every new passkey carries is
// "TimeWarp account · <account fingerprint>" for the account it will belong to — a pre-allocated
// principal for a new account, the signed-in principal for Settings "Create a passkey".
#endregion

#region Design
// Real HTTP with isolated HttpClients (same rationale as PasskeyRegistration_/CredentialAdd_): the
// shared Web.HttpClient's ambient cookie jar may carry an earlier test's session, so every Start
// here goes through a client this test built — anonymous for the new-account path, carrying only
// its own session cookie for the current-account path.
// "The principal created at Complete has the pre-allocated id" is proven from the outside: the
// fingerprint in the name Start sent equals PrincipalFingerprint of the PrincipalId Complete returns
// (the pre-allocated id is never on the wire, so there is nothing else to compare against — that
// absence is itself pinned at contract level by StartPasskeyRegistration_Command_Should).
// The two refusals mirror each other: a current-account challenge cannot mint a new account
// (Complete → 400, no principal) and a new-account challenge cannot add to the caller's account
// (AddPasskey → 400, no credential) — so a stored name always names the account it belongs to.
// "Unused challenge leaves no principal" counts IPrincipalStore principals around Start calls that
// are never completed; expiry of the same state is pinned in the library's challenge-store tests.
#endregion

namespace PasskeyAccountName_;

using System.Buffers.Text;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
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

  public static async Task New_Account_Name_Matches_The_Principal_Complete_Mints()
  {
    using HttpClient client = NewClient(sessionCookie: null);

    (string name, string displayName, byte[] challenge) = await StartAsync(client, forCurrentAccount: false);

    name.ShouldStartWith(PasskeyAccountName.Prefix);
    displayName.ShouldBe(name);
    name.ShouldNotContain("TimeWarp user");

    CompletePasskeyRegistration.Response completed = await CompleteAsync(client, challenge, new IntegrationSoftwareAuthenticator());

    name.ShouldBe(PasskeyAccountName.For(completed.PrincipalId));
  }

  public static async Task Two_New_Accounts_Get_Different_Names()
  {
    using HttpClient client = NewClient(sessionCookie: null);

    (string first, _, byte[] firstChallenge) = await StartAsync(client, forCurrentAccount: false);
    (string second, _, byte[] secondChallenge) = await StartAsync(client, forCurrentAccount: false);

    first.ShouldNotBe(second);

    CompletePasskeyRegistration.Response firstAccount = await CompleteAsync(client, firstChallenge, new IntegrationSoftwareAuthenticator());
    CompletePasskeyRegistration.Response secondAccount = await CompleteAsync(client, secondChallenge, new IntegrationSoftwareAuthenticator());

    firstAccount.PrincipalId.ShouldNotBe(secondAccount.PrincipalId);
    first.ShouldBe(PasskeyAccountName.For(firstAccount.PrincipalId));
    second.ShouldBe(PasskeyAccountName.For(secondAccount.PrincipalId));
  }

  public static async Task Signed_In_Add_Uses_The_Callers_Account_Name_Every_Time()
  {
    (PrincipalId principalId, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    using HttpClient client = NewClient(sessionCookie);
    string expected = PasskeyAccountName.For(principalId);

    (string firstName, string firstDisplayName, byte[] firstChallenge) = await StartAsync(client, forCurrentAccount: true);
    firstName.ShouldBe(expected);
    firstDisplayName.ShouldBe(expected);
    await AddPasskeyAsync(client, firstChallenge);

    (string secondName, _, byte[] secondChallenge) = await StartAsync(client, forCurrentAccount: true);
    secondName.ShouldBe(firstName);
    await AddPasskeyAsync(client, secondChallenge);

    // Settings reads the same fingerprint off the session.
    GetCurrentSession.Response session = await GetSessionAsync(client);
    session.AccountFingerprint.ShouldBe(PrincipalFingerprint.Compute(principalId));
    PasskeyAccountName.Format(session.AccountFingerprint!).ShouldBe(expected);
  }

  public static async Task Unauthorized_Given_ForCurrentAccount_Without_Session()
  {
    using HttpClient client = NewClient(sessionCookie: null);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);

    HttpResponseMessage response = await testApiService.GetHttpResponseMessage
    (
      new StartPasskeyRegistration.Command { ForCurrentAccount = true },
      CancellationToken.None
    );

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task BadRequest_And_No_Principal_Given_Current_Account_Challenge_Used_For_New_Account()
  {
    (PrincipalId _, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    using HttpClient client = NewClient(sessionCookie);
    int before = await CountPrincipalsAsync();

    (_, _, byte[] challenge) = await StartAsync(client, forCurrentAccount: true);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    HttpResponseMessage response = await testApiService.GetHttpResponseMessage
    (
      BuildCompleteCommand(challenge, new IntegrationSoftwareAuthenticator()),
      CancellationToken.None
    );

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    (await CountPrincipalsAsync()).ShouldBe(before);
  }

  public static async Task BadRequest_And_No_Credential_Given_New_Account_Challenge_Used_For_AddPasskey()
  {
    (PrincipalId principalId, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    using HttpClient client = NewClient(sessionCookie);
    int before = await CountCredentialsAsync(principalId);

    (_, _, byte[] challenge) = await StartAsync(client, forCurrentAccount: false);
    (string credentialId, string clientDataJson, string attestationObject) =
      BuildAttestation(challenge, new IntegrationSoftwareAuthenticator());
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    HttpResponseMessage response = await testApiService.GetHttpResponseMessage
    (
      new AddPasskey.Command
      {
        UserId = Guid.NewGuid(),
        CredentialId = credentialId,
        ClientDataJson = clientDataJson,
        AttestationObject = attestationObject
      },
      CancellationToken.None
    );

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    (await CountCredentialsAsync(principalId)).ShouldBe(before);
  }

  public static async Task Unused_Challenges_Leave_No_Principal()
  {
    using HttpClient client = NewClient(sessionCookie: null);
    int before = await CountPrincipalsAsync();

    for (int i = 0; i < 3; i++)
    {
      await StartAsync(client, forCurrentAccount: false);
    }

    (await CountPrincipalsAsync()).ShouldBe(before);
  }

  private static HttpClient NewClient(string? sessionCookie)
  {
    HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    if (sessionCookie is not null)
    {
      client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    }

    return client;
  }

  private static async Task<(string Name, string DisplayName, byte[] Challenge)> StartAsync(HttpClient client, bool forCurrentAccount)
  {
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    HttpResponseMessage response = await testApiService.GetHttpResponseMessage
    (
      new StartPasskeyRegistration.Command { ForCurrentAccount = forCurrentAccount },
      CancellationToken.None
    );
    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    StartPasskeyRegistration.Response? start =
      JsonSerializer.Deserialize<StartPasskeyRegistration.Response>(await response.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    start.ShouldNotBeNull();

    using JsonDocument document = JsonDocument.Parse(start.OptionsJson);
    JsonElement root = document.RootElement;
    JsonElement user = root.GetProperty("user");
    return
    (
      user.GetProperty("name").GetString()!,
      user.GetProperty("displayName").GetString()!,
      Base64Url.DecodeFromChars(root.GetProperty("challenge").GetString()!)
    );
  }

  private static CompletePasskeyRegistration.Command BuildCompleteCommand(byte[] challenge, IntegrationSoftwareAuthenticator authenticator)
  {
    (string credentialId, string clientDataJson, string attestationObject) = BuildAttestation(challenge, authenticator);
    return new CompletePasskeyRegistration.Command
    {
      CredentialId = credentialId,
      ClientDataJson = clientDataJson,
      AttestationObject = attestationObject
    };
  }

  private static async Task<CompletePasskeyRegistration.Response> CompleteAsync
  (
    HttpClient client,
    byte[] challenge,
    IntegrationSoftwareAuthenticator authenticator
  )
  {
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    HttpResponseMessage response =
      await testApiService.GetHttpResponseMessage(BuildCompleteCommand(challenge, authenticator), CancellationToken.None);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    CompletePasskeyRegistration.Response? completed =
      JsonSerializer.Deserialize<CompletePasskeyRegistration.Response>(await response.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    completed.ShouldNotBeNull();
    return completed;
  }

  private static async Task AddPasskeyAsync(HttpClient client, byte[] challenge)
  {
    (string credentialId, string clientDataJson, string attestationObject) =
      BuildAttestation(challenge, new IntegrationSoftwareAuthenticator());
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    HttpResponseMessage response = await testApiService.GetHttpResponseMessage
    (
      new AddPasskey.Command
      {
        UserId = Guid.NewGuid(),
        CredentialId = credentialId,
        ClientDataJson = clientDataJson,
        AttestationObject = attestationObject
      },
      CancellationToken.None
    );

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
  }

  private static (string CredentialId, string ClientDataJson, string AttestationObject) BuildAttestation
  (
    byte[] challenge,
    IntegrationSoftwareAuthenticator authenticator
  )
  {
    string origin = Web.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData("localhost", includeAttestedCredentialData: true);
    byte[] attestationObject = IntegrationSoftwareAuthenticator.BuildAttestationObject(authenticatorData);
    byte[] clientDataJson = IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, origin);

    return
    (
      Base64Url.EncodeToString(authenticator.CredentialId),
      Base64Url.EncodeToString(clientDataJson),
      Base64Url.EncodeToString(attestationObject)
    );
  }

  private static async Task<GetCurrentSession.Response> GetSessionAsync(HttpClient client)
  {
    HttpResponseMessage response = await client.GetAsync(GetCurrentSession.Query.RouteTemplate);
    return JsonSerializer.Deserialize<GetCurrentSession.Response>(await response.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("GetCurrentSession response deserialized to null.");
  }

  private static async Task<int> CountCredentialsAsync(PrincipalId principalId)
  {
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalStore principalStore = scope.ServiceProvider.GetRequiredService<IPrincipalStore>();
    return (await principalStore.ListCredentialsAsync(principalId)).Count;
  }

  private static async Task<int> CountPrincipalsAsync()
  {
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalStore principalStore = scope.ServiceProvider.GetRequiredService<IPrincipalStore>();
    return (await principalStore.ListPrincipalsAsync()).Count;
  }
}
