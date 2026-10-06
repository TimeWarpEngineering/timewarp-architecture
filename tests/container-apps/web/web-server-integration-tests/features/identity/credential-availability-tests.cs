#region Purpose
// GetCredentials availability flags (task 282): CanRevoke only while the caller holds more than one active
// credential, CanRename on every active credential, and CanLinkMicrosoft365 only while Microsoft 365 sign-in
// is offered and none is linked — the same CredentialRules the handlers enforce — and a stale Revoke is still 409.
#endregion

#region Design
// Two halves. CredentialRules_ is host-free and pins the rule table the flags come from, including the
// revoked-Entra case. GetCredentialsAvailability_Returns_ is real HTTP with isolated HttpClients, same
// posture as credential-revoke-tests.cs: only a round trip exercises [EndpointAuthorize], the handler's
// wiring of CredentialRules and EntraSignInOffer, and the real RevokeCredential operation changing the
// flags. Each principal is minted fresh (RegisterPasskeyAndMintSessionAsync starts with one active
// credential; AddPasskey adds more) so tests never share credential counts. The in-proc host starts with
// Entra off; the Microsoft 365 "offered" cases turn it on in-proc for the one test
// (EntraAuthenticationOptions.Enabled + SiteSettings.ReplacePolicy) and restore it afterwards — the
// toggle-and-restore pattern of protected-page-deep-link-tests.cs.
// The stale-Revoke case is the "flags are guidance, the endpoint is the rule" pin: a client holding a
// snapshot where both rows said CanRevoke revokes one, then the other, and the server answers 409.
#endregion

namespace CredentialAvailability_;

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;
using CredentialRules = TimeWarp.Architecture.Features.Identity.Application.CredentialRules;

public class CredentialRules_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CredentialRules_>();

  public static Task Refuse_Revoke_Given_One_Active_Credential()
  {
    CredentialRules.CanRevoke(1).ShouldBeFalse();
    CredentialRules.CanRevoke(0).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Allow_Revoke_Given_Two_Active_Credentials()
  {
    CredentialRules.CanRevoke(2).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Allow_Link_Microsoft365_Only_When_Offered_And_Not_Linked()
  {
    CredentialRules.CanLinkMicrosoft365(true, [CredentialType.Passkey]).ShouldBeTrue();
    CredentialRules.CanLinkMicrosoft365(true, [CredentialType.Passkey, CredentialType.EntraAccount]).ShouldBeFalse();
    CredentialRules.CanLinkMicrosoft365(false, [CredentialType.Passkey]).ShouldBeFalse();
    return Task.CompletedTask;
  }
}

public class GetCredentialsAvailability_Returns_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetCredentialsAvailability_Returns_>();

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

  public static async Task CanRevoke_And_CanRename_On_Each_Credential_Given_Two_Active()
  {
    using HttpClient client = await PrincipalWithCredentialsAsync(2);

    GetCredentials.Response response = await ListAsync(client);

    response.Credentials.Count.ShouldBe(2);
    response.Credentials.ShouldAllBe(static credential => credential.CanRevoke && credential.CanRename);
  }

  public static async Task No_CanRevoke_Given_Exactly_One_Active_Credential()
  {
    using HttpClient client = await PrincipalWithCredentialsAsync(1);

    GetCredentials.Response response = await ListAsync(client);

    GetCredentials.CredentialSummary only = response.Credentials.ShouldHaveSingleItem();
    only.CanRevoke.ShouldBeFalse();
    only.CanRename.ShouldBeTrue();
  }

  public static async Task No_Flags_On_Revoked_Rows_And_CanRevoke_Drops_After_Revoke()
  {
    using HttpClient client = await PrincipalWithCredentialsAsync(2);
    GetCredentials.Response before = await ListAsync(client);
    GetCredentials.CredentialSummary target = before.Credentials[0];

    (await RevokeAsync(client, target.Id.Value)).StatusCode.ShouldBe(HttpStatusCode.OK);

    GetCredentials.Response after = await ListAsync(client, includeRevoked: true);
    after.Credentials.Count.ShouldBe(2);
    GetCredentials.CredentialSummary revoked = after.Credentials.Single(credential => credential.Id == target.Id);
    revoked.IsActive.ShouldBeFalse();
    revoked.CanRevoke.ShouldBeFalse();
    revoked.CanRename.ShouldBeFalse();
    GetCredentials.CredentialSummary remaining = after.Credentials.Single(credential => credential.Id != target.Id);
    remaining.CanRevoke.ShouldBeFalse("the last active credential");
    remaining.CanRename.ShouldBeTrue();
  }

  public static async Task Refuse_A_Stale_Revoke_With_409()
  {
    using HttpClient client = await PrincipalWithCredentialsAsync(2);
    GetCredentials.Response snapshot = await ListAsync(client);
    snapshot.Credentials.ShouldAllBe(static credential => credential.CanRevoke);

    // Act on the old snapshot twice: both rows said CanRevoke, but only the first may go.
    (await RevokeAsync(client, snapshot.Credentials[0].Id.Value)).StatusCode.ShouldBe(HttpStatusCode.OK);
    HttpResponseMessage stale = await RevokeAsync(client, snapshot.Credentials[1].Id.Value);

    stale.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    (await ListAsync(client)).Credentials.ShouldHaveSingleItem().Id.ShouldBe(snapshot.Credentials[1].Id);
  }

  public static async Task No_CanLinkMicrosoft365_Given_Entra_Not_Offered()
  {
    // The in-proc host starts with Entra disabled: GetEntraSignInOffered answers false.
    using HttpClient client = await PrincipalWithCredentialsAsync(1);

    GetCredentials.Response response = await ListAsync(client);

    response.CanLinkMicrosoft365.ShouldBeFalse();
  }

  public static async Task CanLinkMicrosoft365_Given_Entra_Offered_And_Not_Linked()
  {
    using HttpClient client = await PrincipalWithCredentialsAsync(1);
    await using IAsyncDisposable offered = await EnableMicrosoft365OfferedAsync();

    GetCredentials.Response response = await ListAsync(client);

    response.CanLinkMicrosoft365.ShouldBeTrue();
  }

  public static async Task No_CanLinkMicrosoft365_Given_Entra_Offered_And_An_Active_Account_Linked()
  {
    (PrincipalId principalId, HttpClient client) = await PrincipalAsync(1);
    using HttpClient _ = client;
    await AddEntraAccountAsync(principalId);
    await using IAsyncDisposable offered = await EnableMicrosoft365OfferedAsync();

    GetCredentials.Response response = await ListAsync(client);

    response.Credentials.ShouldContain(static credential => credential.Type == CredentialType.EntraAccount && credential.IsActive);
    response.CanLinkMicrosoft365.ShouldBeFalse();
  }

  private static Task<HttpResponseMessage> RevokeAsync(HttpClient client, Guid credentialId)
  {
    var testApiService = new TestApiService(client, ContractSerializationDefaults.Options, bearerToken: null);
    var revoke = new RevokeCredential.Command { UserId = Guid.NewGuid(), CredentialId = credentialId };
    return testApiService.GetHttpResponseMessage(revoke, CancellationToken.None);
  }

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
  // round trip, but the link rule only needs an active EntraAccount row.
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

  private static async Task<GetCredentials.Response> ListAsync(HttpClient client, bool includeRevoked = false)
  {
    var query = new GetCredentials.Query { UserId = Guid.NewGuid(), IncludeRevoked = includeRevoked };
    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    GetCredentials.Response? parsed =
      JsonSerializer.Deserialize<GetCredentials.Response>(await response.Content.ReadAsStringAsync(), ContractSerializationDefaults.Options);
    parsed.ShouldNotBeNull();
    return parsed;
  }
}
