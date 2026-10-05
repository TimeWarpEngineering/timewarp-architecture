#region Purpose
// End-to-end proof of the hypermedia-lab reads (task 275): GetCredentialOffers (approach B) and
// GetCredentialCommands (approach C) reject anonymous callers, offer or command Revoke only while the
// principal holds more than one active credential, follow the real RevokeCredential operation, and
// keep every command href app-relative.
#endregion

#region Design
// Real HTTP with isolated HttpClients, same posture as credential-revoke-tests.cs: only a real
// round trip exercises [EndpointAuthorize] and the shared CredentialRules the handlers consult.
// Each principal is minted fresh (RegisterPasskeyAndMintSessionAsync starts with one active
// credential; AddPasskey adds the second) so tests never share credential counts.
// Approach C's follow-up test posts to the href the server handed out, with the body template the
// server wrote, so a drifted href or body shape fails here instead of in the SPA.
// Link Microsoft 365: the in-proc web-server host does not enable Entra (Authentication:UseEntra is
// false, so GetEntraSignInOffered answers Offered=false). The "offered" side would need a live Entra
// scheme registration and cannot be toggled in-proc; it is covered host-free by the CredentialRules
// predicates, and these tests assert the "not offered" side end to end.
#endregion

namespace HypermediaLabEndpoints_;

using System.Net;
using System.Text;
using System.Text.Json;
using TimeWarp.Architecture.Features.HypermediaLab;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;

internal static class HypermediaLabHelpers
{
  public static HttpClient ClientWithCookie(WebTestServerApplication web, string sessionCookie)
  {
    HttpClient client = new() { BaseAddress = web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    return client;
  }

  /// <summary>Registers a principal with one passkey; adds more until it holds <paramref name="credentialCount"/>.</summary>
  public static async Task<string> PrincipalWithCredentialsAsync(WebTestServerApplication web, int credentialCount)
  {
    (PrincipalId _, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(web);
    using HttpClient client = ClientWithCookie(web, sessionCookie);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    for (int added = 1; added < credentialCount; added++)
    {
      (string credentialId, string clientDataJson, string attestationObject) =
        await CredentialCeremonyHelpers.BuildPasskeyAttestationAsync(web, sessionCookie: sessionCookie);
      var addCommand = new AddPasskey.Command
      {
        UserId = Guid.NewGuid(),
        CredentialId = credentialId,
        ClientDataJson = clientDataJson,
        AttestationObject = attestationObject
      };
      HttpResponseMessage addResponse = await testApiService.GetHttpResponseMessage(addCommand, CancellationToken.None);
      addResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    return sessionCookie;
  }

  public static async Task<T> GetAsync<T>(HttpClient client, string route) where T : class
  {
    HttpResponseMessage response = await client.GetAsync(route);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    T? parsed = JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    parsed.ShouldNotBeNull();
    return parsed;
  }

  public static string[] SubjectsOf(IEnumerable<(string Name, string? Subject)> rows, string name) =>
    [.. rows.Where(row => row.Name == name).Select(row => row.Subject!).Order()];

  public static string[] CredentialIds(IEnumerable<GetCredentials.CredentialSummary> credentials) =>
    [.. credentials.Select(c => c.Id.Value.ToString("D")).Order()];
}

public class GetCredentialOffers_Returns_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;
  private static readonly string Route = new GetCredentialOffers.Query().GetRoute();

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetCredentialOffers_Returns_>();

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

  private static IEnumerable<(string Name, string? Subject)> Rows(GetCredentialOffers.Response response) =>
    response.Offers.Select(o => (o.Name, o.Subject));

  public static async Task Unauthorized_Given_Anonymous_Request()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };

    HttpResponseMessage response = await client.GetAsync(Route);

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Offers_Revoke_And_Rename_For_Each_Credential_Given_Two_Active()
  {
    string cookie = await HypermediaLabHelpers.PrincipalWithCredentialsAsync(Web, 2);
    using HttpClient client = HypermediaLabHelpers.ClientWithCookie(Web, cookie);

    GetCredentialOffers.Response response = await HypermediaLabHelpers.GetAsync<GetCredentialOffers.Response>(client, "/" + Route);

    string[] ids = HypermediaLabHelpers.CredentialIds(response.Credentials);
    ids.Length.ShouldBe(2);
    HypermediaLabHelpers.SubjectsOf(Rows(response), GetCredentialOffers.OfferedActionNames.RevokeCredential).ShouldBe(ids);
    HypermediaLabHelpers.SubjectsOf(Rows(response), GetCredentialOffers.OfferedActionNames.RenameCredential).ShouldBe(ids);
    response.Offers
      .Where(o => o.Subject is not null)
      .ShouldAllBe(o => o.Arguments[GetCredentialOffers.OfferedActionNames.CredentialIdArgument].GetString() == o.Subject);
  }

  public static async Task Offers_No_Revoke_Given_Exactly_One_Active_Credential()
  {
    string cookie = await HypermediaLabHelpers.PrincipalWithCredentialsAsync(Web, 1);
    using HttpClient client = HypermediaLabHelpers.ClientWithCookie(Web, cookie);

    GetCredentialOffers.Response response = await HypermediaLabHelpers.GetAsync<GetCredentialOffers.Response>(client, "/" + Route);

    response.Credentials.Count.ShouldBe(1);
    response.Offers.ShouldNotContain(o => o.Name == GetCredentialOffers.OfferedActionNames.RevokeCredential);
    HypermediaLabHelpers.SubjectsOf(Rows(response), GetCredentialOffers.OfferedActionNames.RenameCredential)
      .ShouldBe(HypermediaLabHelpers.CredentialIds(response.Credentials));
  }

  public static async Task Drops_Revoke_After_Real_Revoke_Leaves_One_Credential()
  {
    string cookie = await HypermediaLabHelpers.PrincipalWithCredentialsAsync(Web, 2);
    using HttpClient client = HypermediaLabHelpers.ClientWithCookie(Web, cookie);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    GetCredentialOffers.Response before = await HypermediaLabHelpers.GetAsync<GetCredentialOffers.Response>(client, "/" + Route);
    GetCredentialOffers.OfferedAction revokeOffer =
      before.Offers.First(o => o.Name == GetCredentialOffers.OfferedActionNames.RevokeCredential);

    var revoke = new RevokeCredential.Command
    {
      UserId = Guid.NewGuid(),
      CredentialId = Guid.Parse(revokeOffer.Arguments[GetCredentialOffers.OfferedActionNames.CredentialIdArgument].GetString()!)
    };
    HttpResponseMessage revokeResponse = await testApiService.GetHttpResponseMessage(revoke, CancellationToken.None);
    revokeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

    GetCredentialOffers.Response after = await HypermediaLabHelpers.GetAsync<GetCredentialOffers.Response>(client, "/" + Route);
    after.Credentials.Count.ShouldBe(1);
    after.Offers.ShouldNotContain(o => o.Name == GetCredentialOffers.OfferedActionNames.RevokeCredential);
    after.Offers.ShouldContain(o => o.Name == GetCredentialOffers.OfferedActionNames.RenameCredential);
  }

  public static async Task Does_Not_Offer_Link_Microsoft365_Given_Entra_Not_Offered()
  {
    // The in-proc host has Entra disabled, so only the "not offered" side is reachable (see Design).
    string cookie = await HypermediaLabHelpers.PrincipalWithCredentialsAsync(Web, 2);
    using HttpClient client = HypermediaLabHelpers.ClientWithCookie(Web, cookie);

    GetCredentialOffers.Response response = await HypermediaLabHelpers.GetAsync<GetCredentialOffers.Response>(client, "/" + Route);

    response.Offers.ShouldNotContain(o => o.Name == GetCredentialOffers.OfferedActionNames.LinkMicrosoft365);
  }
}

public class GetCredentialCommands_Returns_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;
  private static readonly string Route = GetCredentialCommands.SelfHref;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetCredentialCommands_Returns_>();

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

  private static IEnumerable<(string Name, string? Subject)> Rows(GetCredentialCommands.Response response) =>
    response.Commands.Select(c => (c.Rel, c.Subject));

  public static async Task Unauthorized_Given_Anonymous_Request()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };

    HttpResponseMessage response = await client.GetAsync(Route);

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Commands_Revoke_And_Rename_For_Each_Credential_Given_Two_Active()
  {
    string cookie = await HypermediaLabHelpers.PrincipalWithCredentialsAsync(Web, 2);
    using HttpClient client = HypermediaLabHelpers.ClientWithCookie(Web, cookie);

    GetCredentialCommands.Response response = await HypermediaLabHelpers.GetAsync<GetCredentialCommands.Response>(client, Route);

    string[] ids = HypermediaLabHelpers.CredentialIds(response.Credentials);
    ids.Length.ShouldBe(2);
    HypermediaLabHelpers.SubjectsOf(Rows(response), GetCredentialCommands.LinkCommandRels.Revoke).ShouldBe(ids);
    HypermediaLabHelpers.SubjectsOf(Rows(response), GetCredentialCommands.LinkCommandRels.Rename).ShouldBe(ids);
    response.Self.ShouldBe(Route);
  }

  public static async Task Commands_No_Revoke_Given_Exactly_One_Active_Credential()
  {
    string cookie = await HypermediaLabHelpers.PrincipalWithCredentialsAsync(Web, 1);
    using HttpClient client = HypermediaLabHelpers.ClientWithCookie(Web, cookie);

    GetCredentialCommands.Response response = await HypermediaLabHelpers.GetAsync<GetCredentialCommands.Response>(client, Route);

    response.Credentials.Count.ShouldBe(1);
    response.Commands.ShouldNotContain(c => c.Rel == GetCredentialCommands.LinkCommandRels.Revoke);
    HypermediaLabHelpers.SubjectsOf(Rows(response), GetCredentialCommands.LinkCommandRels.Rename)
      .ShouldBe(HypermediaLabHelpers.CredentialIds(response.Credentials));
  }

  public static async Task Every_Command_Href_Is_App_Relative()
  {
    string cookie = await HypermediaLabHelpers.PrincipalWithCredentialsAsync(Web, 2);
    using HttpClient client = HypermediaLabHelpers.ClientWithCookie(Web, cookie);

    GetCredentialCommands.Response response = await HypermediaLabHelpers.GetAsync<GetCredentialCommands.Response>(client, Route);

    response.Commands.ShouldNotBeEmpty();
    foreach (GetCredentialCommands.LinkCommand command in response.Commands)
    {
      command.Href.ShouldStartWith("/");
      command.Href.ShouldNotStartWith("//");
    }
  }

  public static async Task Drops_Revoke_After_Following_Revoke_Command_Href()
  {
    string cookie = await HypermediaLabHelpers.PrincipalWithCredentialsAsync(Web, 2);
    using HttpClient client = HypermediaLabHelpers.ClientWithCookie(Web, cookie);
    GetCredentialCommands.Response before = await HypermediaLabHelpers.GetAsync<GetCredentialCommands.Response>(client, Route);
    GetCredentialCommands.LinkCommand revoke = before.Commands.First(c => c.Rel == GetCredentialCommands.LinkCommandRels.Revoke);
    revoke.Method.ShouldBe(GetCredentialCommands.LinkCommandMethods.Post);

    using HttpRequestMessage request = new(HttpMethod.Post, revoke.Href)
    {
      Content = new StringContent(JsonSerializer.Serialize(revoke.Body, ContractSerializationDefaults.Options), Encoding.UTF8, "application/json")
    };
    HttpResponseMessage revokeResponse = await client.SendAsync(request);
    revokeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

    GetCredentialCommands.Response after = await HypermediaLabHelpers.GetAsync<GetCredentialCommands.Response>(client, before.Self);
    after.Credentials.Count.ShouldBe(1);
    after.Commands.ShouldNotContain(c => c.Rel == GetCredentialCommands.LinkCommandRels.Revoke);
    after.Commands.ShouldContain(c => c.Rel == GetCredentialCommands.LinkCommandRels.Rename);
  }

  public static async Task Does_Not_Command_Link_Microsoft365_Given_Entra_Not_Offered()
  {
    // The in-proc host has Entra disabled, so only the "not offered" side is reachable (see Design).
    string cookie = await HypermediaLabHelpers.PrincipalWithCredentialsAsync(Web, 2);
    using HttpClient client = HypermediaLabHelpers.ClientWithCookie(Web, cookie);

    GetCredentialCommands.Response response = await HypermediaLabHelpers.GetAsync<GetCredentialCommands.Response>(client, Route);

    response.Commands.ShouldNotContain(c => c.Rel == GetCredentialCommands.LinkCommandRels.LinkMicrosoft365);
  }
}
