#region Purpose
// GetCredentials offers (task 279, hypermedia approach B): the server offers Revoke only while the caller
// holds more than one active credential, Rename for every active credential, and Link Microsoft 365 only
// while Microsoft 365 sign-in is offered and none is linked — the same CredentialRules the handlers enforce.
#endregion

#region Design
// Two halves. CredentialOffers_For_ is host-free: CredentialOffers.For over hand-built summaries pins
// the whole rule table, including the revoked-Entra case.
// GetCredentialsOffers_Returns_ is real HTTP with isolated HttpClients, same posture as
// credential-revoke-tests.cs: only a round trip exercises [EndpointAuthorize], the handler's wiring of
// EntraSignInOffer, and the real RevokeCredential operation changing the follow-up set. Each principal
// is minted fresh (RegisterPasskeyAndMintSessionAsync starts with one active credential; AddPasskey
// adds more) so tests never share credential counts. The in-proc host starts with Entra off; the
// Microsoft 365 "offered" cases turn it on in-proc for the one test (EntraAuthenticationOptions.Enabled
// + SiteSettings.ReplacePolicy) and restore it afterwards — the toggle-and-restore pattern of
// protected-page-deep-link-tests.cs — so Link is covered end to end both offered and linked.
#endregion

namespace CredentialOffers_;

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;
using CredentialOffers = TimeWarp.Architecture.Features.Identity.Application.CredentialOffers;

public class CredentialOffers_For_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CredentialOffers_For_>();

  public static Task Offer_No_Revoke_Given_One_Active_Credential()
  {
    GetCredentials.CredentialSummary only = Summary(CredentialType.Passkey);

    IReadOnlyList<OfferedAction> offers = CredentialOffers.For([only], microsoft365Offered: false);

    offers.ShouldNotContain(static offer => offer.Name == OfferedActionNames.RevokeCredential);
    Subjects(offers, OfferedActionNames.RenameCredential).ShouldBe([Id(only)]);
    return Task.CompletedTask;
  }

  public static Task Offer_Revoke_On_Every_Active_Credential_Given_Two_Of_Any_Kind()
  {
    // The page may list one passkey, but the rule counts every active kind — as RevokeCredential.Handler does.
    GetCredentials.CredentialSummary passkey = Summary(CredentialType.Passkey);
    GetCredentials.CredentialSummary agentKey = Summary(CredentialType.AgentKey);

    IReadOnlyList<OfferedAction> offers = CredentialOffers.For([passkey, agentKey], microsoft365Offered: false);

    Subjects(offers, OfferedActionNames.RevokeCredential).ShouldBe([.. new[] { Id(passkey), Id(agentKey) }.Order()]);
    offers.Where(static offer => offer.Subject is not null)
      .ShouldAllBe(offer => offer.Arguments["credentialId"].GetString() == offer.Subject);
    return Task.CompletedTask;
  }

  public static Task Ignore_Revoked_Rows()
  {
    GetCredentials.CredentialSummary active = Summary(CredentialType.Passkey);
    GetCredentials.CredentialSummary revoked = Summary(CredentialType.Passkey, isActive: false);

    IReadOnlyList<OfferedAction> offers = CredentialOffers.For([active, revoked], microsoft365Offered: false);

    offers.ShouldNotContain(static offer => offer.Name == OfferedActionNames.RevokeCredential);
    offers.ShouldNotContain(offer => offer.Subject == Id(revoked));
    return Task.CompletedTask;
  }

  public static Task Offer_Link_Microsoft365_Only_When_Offered_And_Not_Linked()
  {
    GetCredentials.CredentialSummary passkey = Summary(CredentialType.Passkey);
    GetCredentials.CredentialSummary entra = Summary(CredentialType.EntraAccount);
    GetCredentials.CredentialSummary revokedEntra = Summary(CredentialType.EntraAccount, isActive: false);

    bool Link(IReadOnlyList<OfferedAction> offers) =>
      offers.Any(static offer => offer.Name == OfferedActionNames.LinkMicrosoft365 && offer.Subject is null && offer.Arguments.Count == 0);

    Link(CredentialOffers.For([passkey], microsoft365Offered: true)).ShouldBeTrue();
    Link(CredentialOffers.For([passkey, revokedEntra], microsoft365Offered: true)).ShouldBeTrue("a revoked account is not linked");
    Link(CredentialOffers.For([passkey, entra], microsoft365Offered: true)).ShouldBeFalse();
    Link(CredentialOffers.For([passkey], microsoft365Offered: false)).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Name_Only_The_Declared_Vocabulary()
  {
    IReadOnlyList<OfferedAction> offers =
      CredentialOffers.For([Summary(CredentialType.Passkey), Summary(CredentialType.Passkey)], microsoft365Offered: true);

    offers.Select(static offer => offer.Name).Distinct().Order().ShouldBe([.. OfferedActionNames.All.Order()]);
    return Task.CompletedTask;
  }

  private static string[] Subjects(IEnumerable<OfferedAction> offers, string name) =>
    [.. offers.Where(offer => offer.Name == name).Select(static offer => offer.Subject!).Order()];

  private static string Id(GetCredentials.CredentialSummary credential) => credential.Id.Value.ToString("D");

  private static GetCredentials.CredentialSummary Summary(CredentialType type, bool isActive = true) =>
    new
    (
      CredentialId.New(), type, label: null, nickname: null, DateTimeOffset.UtcNow.AddDays(-1),
      revokedAt: isActive ? null : DateTimeOffset.UtcNow, isActive, RegisteredWith.Unknown, "0123abcd"
    );
}

public class GetCredentialsOffers_Returns_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetCredentialsOffers_Returns_>();

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

  public static async Task Revoke_And_Rename_For_Each_Credential_Given_Two_Active()
  {
    using HttpClient client = await PrincipalWithCredentialsAsync(2);

    GetCredentials.Response response = await ListAsync(client);

    string[] ids = [.. response.Credentials.Select(static credential => credential.Id.Value.ToString("D")).Order()];
    ids.Length.ShouldBe(2);
    Subjects(response, OfferedActionNames.RevokeCredential).ShouldBe(ids);
    Subjects(response, OfferedActionNames.RenameCredential).ShouldBe(ids);
  }

  public static async Task No_Revoke_Given_Exactly_One_Active_Credential()
  {
    using HttpClient client = await PrincipalWithCredentialsAsync(1);

    GetCredentials.Response response = await ListAsync(client);

    response.Credentials.Count.ShouldBe(1);
    response.Offers.ShouldNotContain(static offer => offer.Name == OfferedActionNames.RevokeCredential);
    Subjects(response, OfferedActionNames.RenameCredential).ShouldBe([response.Credentials[0].Id.Value.ToString("D")]);
  }

  public static async Task Drop_Revoke_After_The_Offered_Revoke_Leaves_One_Credential()
  {
    using HttpClient client = await PrincipalWithCredentialsAsync(2);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    GetCredentials.Response before = await ListAsync(client);
    OfferedAction revokeOffer = before.Offers.First(static offer => offer.Name == OfferedActionNames.RevokeCredential);

    // Run exactly what the offer names: its credentialId argument against the real endpoint.
    var revoke = new RevokeCredential.Command
    {
      UserId = Guid.NewGuid(),
      CredentialId = Guid.Parse(revokeOffer.Arguments["credentialId"].GetString()!)
    };
    HttpResponseMessage revokeResponse = await testApiService.GetHttpResponseMessage(revoke, CancellationToken.None);
    revokeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

    GetCredentials.Response after = await ListAsync(client);
    after.Credentials.Count.ShouldBe(1);
    after.Offers.ShouldNotContain(static offer => offer.Name == OfferedActionNames.RevokeCredential);
    after.Offers.ShouldContain(static offer => offer.Name == OfferedActionNames.RenameCredential);
  }

  public static async Task No_Link_Microsoft365_Given_Entra_Not_Offered()
  {
    // The in-proc host starts with Entra disabled: GetEntraSignInOffered answers false.
    using HttpClient client = await PrincipalWithCredentialsAsync(1);

    GetCredentials.Response response = await ListAsync(client);

    response.Offers.ShouldNotContain(static offer => offer.Name == OfferedActionNames.LinkMicrosoft365);
  }

  public static async Task Link_Microsoft365_Given_Entra_Offered_And_Not_Linked()
  {
    using HttpClient client = await PrincipalWithCredentialsAsync(1);
    await using IAsyncDisposable offered = await EnableMicrosoft365OfferedAsync();

    GetCredentials.Response response = await ListAsync(client);

    OfferedAction link = response.Offers.Single(static offer => offer.Name == OfferedActionNames.LinkMicrosoft365);
    link.Subject.ShouldBeNull("Link is a page-level offer");
    link.Arguments.ShouldBeEmpty();
  }

  public static async Task No_Link_Microsoft365_Given_Entra_Offered_And_An_Active_Account_Linked()
  {
    (PrincipalId principalId, HttpClient client) = await PrincipalAsync(1);
    using HttpClient _ = client;
    await AddEntraAccountAsync(principalId);
    await using IAsyncDisposable offered = await EnableMicrosoft365OfferedAsync();

    GetCredentials.Response response = await ListAsync(client);

    response.Credentials.ShouldContain(static credential => credential.Type == CredentialType.EntraAccount && credential.IsActive);
    response.Offers.ShouldNotContain(static offer => offer.Name == OfferedActionNames.LinkMicrosoft365);
  }

  private static string[] Subjects(GetCredentials.Response response, string name) =>
    [.. response.Offers.Where(offer => offer.Name == name).Select(static offer => offer.Subject!).Order()];

  /// <summary>A fresh principal holding <paramref name="credentialCount"/> passkeys, as a client carrying its session cookie.</summary>
  private static async Task<HttpClient> PrincipalWithCredentialsAsync(int credentialCount) =>
    (await PrincipalAsync(credentialCount)).Client;

  /// <summary>As <see cref="PrincipalWithCredentialsAsync"/>, also returning the principal's id.</summary>
  private static async Task<(PrincipalId PrincipalId, HttpClient Client)> PrincipalAsync(int credentialCount)
  {
    (PrincipalId principalId, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
    HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    for (int added = 1; added < credentialCount; added++)
    {
      (string credentialId, string clientDataJson, string attestationObject) =
        await CredentialCeremonyHelpers.BuildPasskeyAttestationAsync(Web, sessionCookie: sessionCookie);
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

    return (principalId, client);
  }

  // Store-level insert, same shape as protected-page-deep-link-tests.cs: linking needs a real Entra
  // round trip, but the offer rule only needs an active EntraAccount row.
  private static async Task AddEntraAccountAsync(PrincipalId principalId)
  {
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalStore principalStore = scope.ServiceProvider.GetRequiredService<IPrincipalStore>();
    Guid tenantId = Guid.NewGuid();
    await principalStore.AddCredentialAsync(
      Credential.Create(
        principalId,
        CredentialType.EntraAccount,
        EntraAccountHandle.Encode(tenantId, Guid.NewGuid()),
        EntraIssuerMaterial.FromTenantId(tenantId),
        label: "Microsoft 365",
        accountHint: "linked@example.test"));
  }

  /// <summary>Turns Microsoft 365 sign-in on in-proc (scheme enabled + site policy on); disposing restores both.</summary>
  private static async Task<IAsyncDisposable> EnableMicrosoft365OfferedAsync()
  {
    AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    EntraAuthenticationOptions options =
      scope.ServiceProvider.GetRequiredService<IOptions<EntraAuthenticationOptions>>().Value;
    ISiteSettingsStore store = scope.ServiceProvider.GetRequiredService<ISiteSettingsStore>();
    SiteSettings settings = (await store.GetAsync()).ShouldNotBeNull();
    bool previousEnabled = options.Enabled;
    bool previousSignIn = settings.EntraSignInEnabled;
    bool previousBootstrap = settings.EntraAllowBootstrap;
    PasskeyPromptMode previousMode = settings.PasskeyPromptMode;
    options.Enabled = true;
    settings.ReplacePolicy(true, previousBootstrap, previousMode);
    await store.UpdateAsync(settings);
    return new Restore
    (
      async () =>
      {
        options.Enabled = previousEnabled;
        SiteSettings restore = (await store.GetAsync()).ShouldNotBeNull();
        restore.ReplacePolicy(previousSignIn, previousBootstrap, previousMode);
        await store.UpdateAsync(restore);
        await scope.DisposeAsync();
      }
    );
  }

  private sealed class Restore(Func<Task> restore) : IAsyncDisposable
  {
    public async ValueTask DisposeAsync() => await restore();
  }

  private static async Task<GetCredentials.Response> ListAsync(HttpClient client)
  {
    var query = new GetCredentials.Query { UserId = Guid.NewGuid() };
    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    GetCredentials.Response? parsed =
      JsonSerializer.Deserialize<GetCredentials.Response>(await response.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    parsed.ShouldNotBeNull();
    return parsed;
  }
}
